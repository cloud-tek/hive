using Refit;

namespace Hive.HTTP;

public static class Startup
{
  // --- IMicroService overloads (preferred by compiler for MicroService) ---

  public static IMicroService WithHttpClient<TApi>(this IMicroService service)
    where TApi : class
  {
    WithHttpClientCore<TApi>(service, typeof(TApi).Name, null);
    return service;
  }

  public static IMicroService WithHttpClient<TApi>(
    this IMicroService service,
    Action<HiveHttpClientBuilder> configure)
    where TApi : class
  {
    WithHttpClientCore<TApi>(service, typeof(TApi).Name, configure);
    return service;
  }

  public static IMicroService WithHttpClient<TApi>(
    this IMicroService service,
    string clientName)
    where TApi : class
  {
    WithHttpClientCore<TApi>(service, clientName, null);
    return service;
  }

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

  public static IMicroServiceCore WithHttpClient<TApi>(this IMicroServiceCore service)
    where TApi : class
  {
    WithHttpClientCore<TApi>(service, typeof(TApi).Name, null);
    return service;
  }

  public static IMicroServiceCore WithHttpClient<TApi>(
    this IMicroServiceCore service,
    Action<HiveHttpClientBuilder> configure)
    where TApi : class
  {
    WithHttpClientCore<TApi>(service, typeof(TApi).Name, configure);
    return service;
  }

  public static IMicroServiceCore WithHttpClient<TApi>(
    this IMicroServiceCore service,
    string clientName)
    where TApi : class
  {
    WithHttpClientCore<TApi>(service, clientName, null);
    return service;
  }

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