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
  /// <param name="otelExporterOtlpEnvpointEnvVar"></param>
  public Extension(IMicroService service,
    Action<LoggerProviderBuilder>? logging = null,
    Action<TracerProviderBuilder>? tracing = null,
    Action<MeterProviderBuilder>? metrics = null,
    string otelExporterOtlpEnvpointEnvVar = Constants.Environment.OtelExporterOtlpEndpoint) : base(service)
  {
    ConfigureActions.Add((svc, cfg) =>
    {
      //_ = svc.ConfigureOptions<OtlpExporterOptions>(cfg,);

      //tracing.
      // register otel
      svc.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(
          serviceName: service.Name,
          serviceNamespace: null,
          serviceInstanceId: service.Id,
          serviceVersion: null,
          autoGenerateServiceInstanceId: false))
        .WithLogging(
          configure: logging ?? ((log) =>
          {
            log.AddConsoleExporter();

            if (service.EnvironmentVariables.ContainsKey(otelExporterOtlpEnvpointEnvVar))
            {
              log.AddOtlpExporter(options =>
              {
                options.Endpoint = new Uri(
                  service.EnvironmentVariables[otelExporterOtlpEnvpointEnvVar]);
              });
            }
          }))
      .WithTracing(configure: tracing ?? ((trace) =>
        {
          trace.AddAspNetCoreInstrumentation();
          trace.AddHttpClientInstrumentation();

          if (service.EnvironmentVariables.ContainsKey(otelExporterOtlpEnvpointEnvVar))
          {
            trace.AddOtlpExporter(options =>
            {
              options.Endpoint = new Uri(service.EnvironmentVariables[otelExporterOtlpEnvpointEnvVar]);
            });
          }
        }))
      .WithMetrics(configure: metrics ?? (meter =>
      {
        meter.AddAspNetCoreInstrumentation();
        meter.AddHttpClientInstrumentation();
        meter.AddRuntimeInstrumentation();

        if (service.EnvironmentVariables.ContainsKey(otelExporterOtlpEnvpointEnvVar))
        {
          meter.AddOtlpExporter(options =>
          {
            options.Endpoint = new Uri(service.EnvironmentVariables[otelExporterOtlpEnvpointEnvVar]);
          });
        }
      }));
    });
  }
}