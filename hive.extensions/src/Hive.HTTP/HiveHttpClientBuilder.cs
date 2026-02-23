using Hive.HTTP.Authentication;
using Hive.HTTP.Resilience;

namespace Hive.HTTP;

/// <summary>
/// Fluent builder for configuring an HTTP client registration.
/// </summary>
public sealed class HiveHttpClientBuilder
{
  internal HttpClientRegistration Registration { get; }

  internal HiveHttpClientBuilder(HttpClientRegistration registration)
  {
    Registration = registration;
  }

  /// <summary>
  /// Marks the client as internal (service-to-service communication). This is the default.
  /// </summary>
  /// <returns>The builder instance for chaining.</returns>
  public HiveHttpClientBuilder Internal()
  {
    Registration.FluentFlavour = HttpClientFlavour.Internal;
    return this;
  }

  /// <summary>
  /// Marks the client as external (third-party API communication).
  /// </summary>
  /// <returns>The builder instance for chaining.</returns>
  public HiveHttpClientBuilder External()
  {
    Registration.FluentFlavour = HttpClientFlavour.External;
    return this;
  }

  /// <summary>
  /// Sets the base address for the HTTP client. Overrides any value from JSON configuration.
  /// </summary>
  /// <param name="baseAddress">The base URL for all requests.</param>
  /// <returns>The builder instance for chaining.</returns>
  public HiveHttpClientBuilder WithBaseAddress(string baseAddress)
  {
    ArgumentNullException.ThrowIfNull(baseAddress);
    Registration.FluentBaseAddress = baseAddress;
    return this;
  }

  /// <summary>
  /// Configures authentication for the HTTP client.
  /// </summary>
  /// <param name="configure">A delegate to configure the authentication builder.</param>
  /// <returns>The builder instance for chaining.</returns>
  public HiveHttpClientBuilder WithAuthentication(Action<AuthenticationBuilder> configure)
  {
    ArgumentNullException.ThrowIfNull(configure);

    var builder = new AuthenticationBuilder();
    configure(builder);

    Registration.AuthenticationProviderFactory = builder.ProviderFactory;
    if (builder.AuthenticationType is not null)
      Registration.AuthenticationType = builder.AuthenticationType;

    return this;
  }

  /// <summary>
  /// Configures resilience policies (retry, circuit breaker, timeout) for the HTTP client.
  /// </summary>
  /// <param name="configure">A delegate to configure the resilience builder.</param>
  /// <returns>The builder instance for chaining.</returns>
  public HiveHttpClientBuilder WithResilience(Action<ResilienceBuilder> configure)
  {
    ArgumentNullException.ThrowIfNull(configure);

    var builder = new ResilienceBuilder();
    configure(builder);

    Registration.FluentResilience = builder.Options;
    return this;
  }

  /// <summary>
  /// Adds a custom <see cref="DelegatingHandler"/> to the HTTP client pipeline.
  /// </summary>
  /// <typeparam name="THandler">The handler type, resolved from the service provider.</typeparam>
  /// <returns>The builder instance for chaining.</returns>
  public HiveHttpClientBuilder WithHandler<THandler>() where THandler : DelegatingHandler
  {
    Registration.CustomHandlerTypes.Add(typeof(THandler));
    return this;
  }

  /// <summary>
  /// Sets custom Refit serialization and deserialization settings.
  /// </summary>
  /// <param name="settings">The Refit settings to use.</param>
  /// <returns>The builder instance for chaining.</returns>
  public HiveHttpClientBuilder WithRefitSettings(Refit.RefitSettings settings)
  {
    ArgumentNullException.ThrowIfNull(settings);
    Registration.RefitSettings = settings;
    return this;
  }
}
