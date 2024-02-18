namespace Hive.Logging;

/// <summary>
/// Extension methods for the logging service.
/// </summary>
public static class Startup
{
  /// <summary>
  /// Adds logging to the service.
  /// </summary>
  /// <param name="service"></param>
  /// <param name="log"></param>
  /// <returns><see cref="IMicroService"/></returns>
  public static IMicroService WithLogging(this IMicroService service, Action<LoggingConfigurationBuilder> log)
  {
    service.Extensions.Add(new Extension(service, log));

    return service;
  }
}