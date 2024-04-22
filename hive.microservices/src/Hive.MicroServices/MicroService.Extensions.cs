using Hive.MicroServices.CORS;

namespace Hive.MicroServices;

/// <summary>
/// The extensions for a microservice.
/// </summary>
public static class MicroServiceExtensions
{
  /// <summary>
  /// Adds CORS to the microservice
  /// </summary>
  /// <param name="service"></param>
  /// <param name="urls"></param>
  /// <returns><see cref="IMicroService"/></returns>
  public static IMicroService WithCORS(this IMicroService service, params string[] urls)
  {
    service.Extensions.Add(new Extension(service));

    return service;
  }
}