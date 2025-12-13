using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Hive.OpenTelemetry;

/// <summary>
///
/// </summary>
public class Extension : MicroServiceExtension
{
  /// <summary>
  ///
  /// </summary>
  /// <param name="service"></param>
  /// <param name="logging"></param>
  /// <param name="tracing"></param>
  /// <param name="metrics"></param>
  public Extension(IMicroService service,
    Action<LoggerProviderBuilder> logging,
    Action<TracerProviderBuilder> tracing,
    Action<MeterProviderBuilder> metrics) : base(service)
  {
    ConfigureActions.Add((svc, cfg) =>
    {
      // register otel
      svc.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(
          serviceName: service.Name,
          serviceNamespace: null,
          serviceInstanceId: service.Id,
          serviceVersion: null,
          autoGenerateServiceInstanceId: false))
        .WithLogging(configure: logging)
        .WithTracing(configure: tracing)
        .WithMetrics(configure: metrics);
    });
  }
}