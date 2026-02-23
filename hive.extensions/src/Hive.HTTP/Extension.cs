using FluentValidation;
using Hive.HTTP.Authentication;
using Hive.HTTP.Configuration;
using Hive.HTTP.Lifecycle;
using Hive.HTTP.Telemetry;
using Hive.MicroServices.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Refit;

namespace Hive.HTTP;

/// <summary>
/// Hive extension that registers and configures Refit-based HTTP clients with telemetry, authentication, and resilience.
/// </summary>
public sealed class Extension : MicroServiceExtension<Extension>
{
  private static readonly HttpClientRegistrationValidator Validator = new();
  private readonly List<HttpClientRegistration> _registrations = [];
  internal Dictionary<string, Func<HttpMessageHandler>> PrimaryHandlerOverrides { get; } = new();

  /// <summary>
  /// Initializes a new instance of the <see cref="Extension"/> class.
  /// </summary>
  /// <param name="service">The microservice this extension is registered with.</param>
  public Extension(IMicroServiceCore service) : base(service)
  {
  }

  internal void AddRegistration(HttpClientRegistration registration)
  {
    _registrations.Add(registration);
  }

  /// <inheritdoc />
  public override IServiceCollection ConfigureServices(
    IServiceCollection services,
    IMicroServiceCore microservice)
  {
    ConfigureActions.Add((svc, configuration) =>
    {
      var httpSection = configuration.GetSection(HttpClientOptions.SectionKey);
      var configuredClients = new Dictionary<string, HttpClientOptions>();

      if (httpSection.Exists())
      {
        foreach (var child in httpSection.GetChildren())
        {
          var clientOptions = new HttpClientOptions();
          child.Bind(clientOptions);
          configuredClients[child.Key] = clientOptions;
        }
      }

      var validationFailures = new List<ValidationException>();

      foreach (var registration in _registrations)
      {
        var clientName = registration.ClientName;

        if (configuredClients.TryGetValue(clientName, out var clientConfig))
        {
          registration.ApplyConfiguration(clientConfig);
        }

        registration.ApplyFluentOverrides();

        var result = Validator.Validate(registration);

        if (!result.IsValid)
        {
          validationFailures.Add(new ValidationException(result.Errors));
          continue;
        }

        RegisterClient(svc, registration, microservice, this);
      }

      if (validationFailures.Count > 0)
      {
        svc.AddSingleton<IHostedStartupService>(
          new HttpClientValidationStartupService(validationFailures));
      }
    });

    return services;
  }

  private static void RegisterClient(
    IServiceCollection services,
    HttpClientRegistration registration,
    IMicroServiceCore microservice,
    Extension extension)
  {
    var clientName = registration.ClientName;

    var refitSettings = registration.RefitSettings ?? new RefitSettings();
    var httpClientBuilder = registration.RefitClientFactory(services, refitSettings, clientName);

    httpClientBuilder.ConfigureHttpClient(c =>
    {
      c.BaseAddress = new Uri(registration.BaseAddress!);
    });

    if (extension.PrimaryHandlerOverrides.TryGetValue(clientName, out var handlerFactory))
    {
      httpClientBuilder.ConfigurePrimaryHttpMessageHandler(handlerFactory);
    }
    else
    {
      httpClientBuilder.UseSocketsHttpHandler((handler, _) =>
      {
        handler.PooledConnectionLifetime = registration.SocketsHandler.PooledConnectionLifetime;
        handler.PooledConnectionIdleTimeout = registration.SocketsHandler.PooledConnectionIdleTimeout;
        handler.MaxConnectionsPerServer = registration.SocketsHandler.MaxConnectionsPerServer;
      });
    }

    // Outermost handler: telemetry
    httpClientBuilder.AddHttpMessageHandler(() =>
      new TelemetryHandler(microservice.Name, clientName));

    // Authentication handler
    if (registration.AuthenticationProviderFactory is not null)
    {
      httpClientBuilder.AddHttpMessageHandler(sp =>
      {
        var provider = registration.AuthenticationProviderFactory(sp);
        return new AuthenticationHandler(provider);
      });
    }
    else if (registration.AuthenticationType == "ApiKey"
             && registration.AuthenticationHeaderName is not null
             && registration.AuthenticationValue is not null)
    {
      var apiKeyProvider = new ApiKeyProvider(
        registration.AuthenticationHeaderName,
        registration.AuthenticationValue);
      httpClientBuilder.AddHttpMessageHandler(() =>
        new AuthenticationHandler(apiKeyProvider));
    }

    // Custom handlers
    foreach (var handlerType in registration.CustomHandlerTypes)
    {
      httpClientBuilder.AddHttpMessageHandler(sp =>
        (DelegatingHandler)sp.GetRequiredService(handlerType));
    }

    // Resilience pipeline
    ConfigureResilience(httpClientBuilder, registration, clientName);
  }

  private static void ConfigureResilience(
    IHttpClientBuilder builder,
    HttpClientRegistration registration,
    string clientName)
  {
    var resilience = registration.Resilience;
    var hasResilience = resilience.MaxRetries.HasValue
                        || resilience.PerAttemptTimeout.HasValue
                        || resilience.CircuitBreaker is { Enabled: true };

    if (!hasResilience)
      return;

    var clientTag = new KeyValuePair<string, object?>("client.name", clientName);

    builder.AddResilienceHandler(clientName, resilienceBuilder =>
    {
      if (resilience.MaxRetries.HasValue)
      {
        resilienceBuilder.AddRetry(new HttpRetryStrategyOptions
        {
          MaxRetryAttempts = resilience.MaxRetries.Value,
          OnRetry = args =>
          {
            HttpClientMeter.ResilienceRetries.Add(1, clientTag);
            return default;
          }
        });
      }

      if (resilience.CircuitBreaker is { Enabled: true })
      {
        var cbOptions = new HttpCircuitBreakerStrategyOptions
        {
          FailureRatio = resilience.CircuitBreaker.FailureRatio,
          SamplingDuration = resilience.CircuitBreaker.SamplingDuration,
          MinimumThroughput = resilience.CircuitBreaker.MinimumThroughput,
          BreakDuration = resilience.CircuitBreaker.BreakDuration,
          OnOpened = args =>
          {
            HttpClientMeter.CircuitBreakerStateChanges.Add(1, clientTag,
              new KeyValuePair<string, object?>("state", "open"));
            return default;
          },
          OnClosed = args =>
          {
            HttpClientMeter.CircuitBreakerStateChanges.Add(1, clientTag,
              new KeyValuePair<string, object?>("state", "closed"));
            return default;
          },
          OnHalfOpened = args =>
          {
            HttpClientMeter.CircuitBreakerStateChanges.Add(1, clientTag,
              new KeyValuePair<string, object?>("state", "half-open"));
            return default;
          }
        };
        resilienceBuilder.AddCircuitBreaker(cbOptions);
      }

      if (resilience.PerAttemptTimeout.HasValue)
      {
        resilienceBuilder.AddTimeout(resilience.PerAttemptTimeout.Value);
      }
    });
  }
}
