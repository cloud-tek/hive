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
        .WithLogging(configure: (log) =>
        {
          log.AddConsoleExporter();

          if (service.EnvironmentVariables.ContainsKey(Constants.Environment.OtelExporterOtlpEndpoint))
          {
            log.AddOtlpExporter(options =>
            {
              options.Endpoint = new Uri(service.EnvironmentVariables[Constants.Environment.OtelExporterOtlpEndpoint]);
            });
          }

          if (logging != null)
          {
            logging(log);
          }
        })
        .WithTracing(configure: tracing)
        .WithMetrics(configure: m =>
        {
          m.AddAspNetCoreInstrumentation();
          m.AddHttpClientInstrumentation();
          m.AddRuntimeInstrumentation();

          if (metrics != null)
          {
            metrics(m);
          }

          if (service.EnvironmentVariables.ContainsKey(Constants.Environment.OtelExporterOtlpEndpoint))
          {
            m.AddOtlpExporter(options =>
            {
              options.Endpoint = new Uri(service.EnvironmentVariables[Constants.Environment.OtelExporterOtlpEndpoint]);
            });
          }
        });
    });
  }
}