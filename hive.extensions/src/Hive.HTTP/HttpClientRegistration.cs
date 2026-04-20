using Hive.HTTP.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.HTTP;

internal sealed class HttpClientRegistration
{
  public required string ClientName { get; init; }

  public required Type InterfaceType { get; init; }

  public string? BaseAddress { get; set; }

  public HttpClientFlavour Flavour { get; set; } = HttpClientFlavour.Internal;

  public ResilienceOptions Resilience { get; set; } = new();

  public string? AuthenticationType { get; set; }

  public string? AuthenticationHeaderName { get; set; }

  public string? AuthenticationValue { get; set; }

  public Func<IServiceProvider, Authentication.IAuthenticationProvider>? AuthenticationProviderFactory { get; set; }

  public SocketsHandlerOptions SocketsHandler { get; set; } = new();

  public List<Type> CustomHandlerTypes { get; } = [];

  public Refit.RefitSettings? RefitSettings { get; set; }

  /// <summary>
  /// Factory captured at registration time while TApi is still known,
  /// avoiding the need for reflection when calling AddRefitClient.
  /// </summary>
  public required Func<IServiceCollection, Refit.RefitSettings, string, IHttpClientBuilder> RefitClientFactory { get; init; }

  // Fluent overrides (stored separately, applied in ApplyFluentOverrides)
  internal string? FluentBaseAddress { get; set; }

  internal HttpClientFlavour? FluentFlavour { get; set; }

  internal ResilienceOptions? FluentResilience { get; set; }

  internal SocketsHandlerOptions? FluentSocketsHandler { get; set; }

  public void ApplyConfiguration(HttpClientOptions options)
  {
    if (!string.IsNullOrEmpty(options.BaseAddress))
      BaseAddress = options.BaseAddress;

    Flavour = options.Flavour;

    if (options.Resilience.MaxRetries.HasValue)
      Resilience.MaxRetries = options.Resilience.MaxRetries;

    if (options.Resilience.PerAttemptTimeout.HasValue)
      Resilience.PerAttemptTimeout = options.Resilience.PerAttemptTimeout;

    if (options.Resilience.CircuitBreaker is not null)
      Resilience.CircuitBreaker = options.Resilience.CircuitBreaker;

    if (options.Authentication is not null)
    {
      AuthenticationType = options.Authentication.Type;
      AuthenticationHeaderName = options.Authentication.HeaderName;
      AuthenticationValue = options.Authentication.Value;
    }

    SocketsHandler = new SocketsHandlerOptions
    {
      PooledConnectionLifetime = options.SocketsHandler.PooledConnectionLifetime,
      PooledConnectionIdleTimeout = options.SocketsHandler.PooledConnectionIdleTimeout,
      MaxConnectionsPerServer = options.SocketsHandler.MaxConnectionsPerServer
    };
  }

  public void ApplyFluentOverrides()
  {
    if (FluentBaseAddress is not null)
      BaseAddress = FluentBaseAddress;

    if (FluentFlavour.HasValue)
      Flavour = FluentFlavour.Value;

    if (FluentResilience is not null)
    {
      if (FluentResilience.MaxRetries.HasValue)
        Resilience.MaxRetries = FluentResilience.MaxRetries;

      if (FluentResilience.PerAttemptTimeout.HasValue)
        Resilience.PerAttemptTimeout = FluentResilience.PerAttemptTimeout;

      if (FluentResilience.CircuitBreaker is not null)
        Resilience.CircuitBreaker = FluentResilience.CircuitBreaker;
    }

    if (FluentSocketsHandler is not null)
      SocketsHandler = FluentSocketsHandler;
  }
}