using Refit;

namespace Hive.HTTP;

/// <summary>
/// Extension methods for registering Refit-based HTTP clients with the Hive microservice.
/// </summary>
public static class Startup
{
  // --- IMicroService overloads (preferred by compiler for MicroService) ---

  /// <summary>
  /// Registers a typed HTTP client for the specified Refit interface using the interface name as the client name.
  /// </summary>
  /// <typeparam name="TApi">The Refit interface defining the HTTP API.</typeparam>
  /// <param name="service">The microservice to register the client with.</param>
  /// <returns>The microservice instance for chaining.</returns>
  public static IMicroService WithHttpClient<TApi>(this IMicroService service)
    where TApi : class
  {
    WithHttpClientCore<TApi>(service, typeof(TApi).Name, null);
    return service;
  }

  /// <summary>
  /// Registers a typed HTTP client with fluent configuration using the interface name as the client name.
  /// </summary>
  /// <typeparam name="TApi">The Refit interface defining the HTTP API.</typeparam>
  /// <param name="service">The microservice to register the client with.</param>
  /// <param name="configure">A delegate to configure the client builder.</param>
  /// <returns>The microservice instance for chaining.</returns>
  public static IMicroService WithHttpClient<TApi>(
    this IMicroService service,
    Action<HiveHttpClientBuilder> configure)
    where TApi : class
  {
    WithHttpClientCore<TApi>(service, typeof(TApi).Name, configure);
    return service;
  }

  /// <summary>
  /// Registers a typed HTTP client with a custom client name.
  /// </summary>
  /// <typeparam name="TApi">The Refit interface defining the HTTP API.</typeparam>
  /// <param name="service">The microservice to register the client with.</param>
  /// <param name="clientName">The name used to identify this client in configuration and telemetry.</param>
  /// <returns>The microservice instance for chaining.</returns>
  public static IMicroService WithHttpClient<TApi>(
    this IMicroService service,
    string clientName)
    where TApi : class
  {
    WithHttpClientCore<TApi>(service, clientName, null);
    return service;
  }

  /// <summary>
  /// Registers a typed HTTP client with a custom client name and fluent configuration.
  /// </summary>
  /// <typeparam name="TApi">The Refit interface defining the HTTP API.</typeparam>
  /// <param name="service">The microservice to register the client with.</param>
  /// <param name="clientName">The name used to identify this client in configuration and telemetry.</param>
  /// <param name="configure">An optional delegate to configure the client builder.</param>
  /// <returns>The microservice instance for chaining.</returns>
  public static IMicroService WithHttpClient<TApi>(
    this IMicroService service,
    string clientName,
    Action<HiveHttpClientBuilder>? configure)
    where TApi : class
  {
    WithHttpClientCore<TApi>(service, clientName, configure);
    return service;
  }

  // --- IMicroServiceCore overloads (for FunctionHost and other hosts) ---

  /// <inheritdoc cref="WithHttpClient{TApi}(IMicroService)"/>
  public static IMicroServiceCore WithHttpClient<TApi>(this IMicroServiceCore service)
    where TApi : class
  {
    WithHttpClientCore<TApi>(service, typeof(TApi).Name, null);
    return service;
  }

  /// <inheritdoc cref="WithHttpClient{TApi}(IMicroService, Action{HiveHttpClientBuilder})"/>
  public static IMicroServiceCore WithHttpClient<TApi>(
    this IMicroServiceCore service,
    Action<HiveHttpClientBuilder> configure)
    where TApi : class
  {
    WithHttpClientCore<TApi>(service, typeof(TApi).Name, configure);
    return service;
  }

  /// <inheritdoc cref="WithHttpClient{TApi}(IMicroService, string)"/>
  public static IMicroServiceCore WithHttpClient<TApi>(
    this IMicroServiceCore service,
    string clientName)
    where TApi : class
  {
    WithHttpClientCore<TApi>(service, clientName, null);
    return service;
  }

  /// <inheritdoc cref="WithHttpClient{TApi}(IMicroService, string, Action{HiveHttpClientBuilder})"/>
  public static IMicroServiceCore WithHttpClient<TApi>(
    this IMicroServiceCore service,
    string clientName,
    Action<HiveHttpClientBuilder>? configure)
    where TApi : class
  {
    WithHttpClientCore<TApi>(service, clientName, configure);
    return service;
  }

  // --- Shared implementation ---

  private static void WithHttpClientCore<TApi>(
    IMicroServiceCore service,
    string clientName,
    Action<HiveHttpClientBuilder>? configure)
    where TApi : class
  {
    ArgumentNullException.ThrowIfNull(service);
    ArgumentNullException.ThrowIfNull(clientName);

    var extension = service.Extensions
      .OfType<Extension>()
      .FirstOrDefault();

    if (extension is null)
    {
      extension = new Extension(service);
      service.Extensions.Add(extension);
    }

    var registration = new HttpClientRegistration
    {
      ClientName = clientName,
      InterfaceType = typeof(TApi),
      RefitClientFactory = (services, settings, name) =>
        services.AddRefitClient<TApi>(settings, name)
    };

    if (configure is not null)
    {
      var builder = new HiveHttpClientBuilder(registration);
      configure(builder);
    }

    extension.AddRegistration(registration);
  }
}