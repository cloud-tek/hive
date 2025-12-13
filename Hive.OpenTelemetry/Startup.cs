using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Hive.OpenTelemetry;

/// <summary>
///
/// </summary>
public static class MicroServiceExtensions
{
  /// <summary>
  ///
  /// </summary>
  /// <param name="service"></param>
  /// <param name="logging"></param>
  /// <param name="tracing"></param>
  /// <param name="metrics"></param>
  /// <returns></returns>
  public static IMicroService WithOpenTelemetry(
    this IMicroService service,
    Action<LoggerProviderBuilder> logging,
    Action<TracerProviderBuilder> tracing,
    Action<MeterProviderBuilder> metrics)
  {
    service.Extensions.Add(
      new Extension(
        service: service,
        logging: logging,
        tracing: tracing,
        metrics: metrics));
    return service;
  }
}