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
  /// <param name="otelExporterOtlpEnvpointEnvVar"></param>
  /// <returns></returns>
  public static IMicroService WithOpenTelemetry(
    this IMicroService service,
    Action<LoggerProviderBuilder>? logging = null,
    Action<TracerProviderBuilder>? tracing = null,
    Action<MeterProviderBuilder>? metrics = null,
    string otelExporterOtlpEnvpointEnvVar = Constants.Environment.OtelExporterOtlpEndpoint)
  {
    service.Extensions.Add(
      new Extension(
        service: service,
        logging: logging,
        tracing: tracing,
        metrics: metrics,
        otelExporterOtlpEnvpointEnvVar: otelExporterOtlpEnvpointEnvVar));
    return service;
  }
}