using Hive.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Hive.OpenTelemetry;

/// <summary>
/// OpenTelemetry extension for Hive microservices
/// </summary>
public class Extension : MicroServiceExtension
{
  private readonly Action<LoggerProviderBuilder>? loggingOverride;
  private readonly Action<TracerProviderBuilder>? tracingOverride;
  private readonly Action<MeterProviderBuilder>? metricsOverride;

  /// <summary>
  /// Initializes a new instance of the OpenTelemetry extension
  /// </summary>
  /// <param name="service">The microservice instance</param>
  /// <param name="logging">Optional logging configuration override</param>
  /// <param name="tracing">Optional tracing configuration override</param>
  /// <param name="metrics">Optional metrics configuration override</param>
  public Extension(
    IMicroService service,
    Action<LoggerProviderBuilder>? logging = null,
    Action<TracerProviderBuilder>? tracing = null,
    Action<MeterProviderBuilder>? metrics = null)
    : base(service)
  {
    loggingOverride = logging;
    tracingOverride = tracing;
    metricsOverride = metrics;

    ConfigureActions.Add((svc, cfg) =>
    {
      // Load options from IConfiguration with defaults
      var options = LoadOptionsFromConfiguration(svc, cfg);

      // Resolve OTLP endpoint with fallback chain
      var otlpEndpoint = ResolveOtlpEndpoint(options, service);

      // Configure OpenTelemetry
      svc.AddOpenTelemetry()
        .ConfigureResource(resource => ConfigureResource(resource, service, options))
        .WithLogging(builder => ConfigureLogging(builder, options, otlpEndpoint))
        .WithTracing(builder => ConfigureTracing(builder, options, otlpEndpoint))
        .WithMetrics(builder => ConfigureMetrics(builder, options, otlpEndpoint));
    });
  }

  private static OpenTelemetryOptions LoadOptionsFromConfiguration(
    IServiceCollection services,
    IConfiguration configuration)
  {
    // Use PreConfigureOptionalValidatedOptions since the configuration section is optional
    // Returns null if section doesn't exist, allowing us to use defaults
    var optionsInstance = services.PreConfigureOptionalValidatedOptions<OpenTelemetryOptions, OptionsValidator>(
      configuration,
      () => OpenTelemetryOptions.SectionKey);

    // Return validated options if configuration exists, otherwise use defaults
    return optionsInstance?.Value ?? new OpenTelemetryOptions();
  }

  private static string? ResolveOtlpEndpoint(OpenTelemetryOptions options, IMicroService service)
  {
    // Priority order:
    // 1. IConfiguration (options.Otlp.Endpoint)
    // 2. Environment variable OTEL_EXPORTER_OTLP_ENDPOINT
    // 3. null (no OTLP export)

    if (!string.IsNullOrWhiteSpace(options.Otlp.Endpoint))
    {
      return options.Otlp.Endpoint;
    }

    if (service.EnvironmentVariables?.TryGetValue(
      Constants.Environment.OtelExporterOtlpEndpoint,
      out var envEndpoint) == true && !string.IsNullOrWhiteSpace(envEndpoint))
    {
      return envEndpoint;
    }

    return null;
  }

  private static ResourceBuilder ConfigureResource(
    ResourceBuilder resource,
    IMicroService service,
    OpenTelemetryOptions options)
  {
    resource.AddService(
      serviceName: service.Name,
      serviceNamespace: options.Resource.ServiceNamespace,
      serviceInstanceId: service.Id,
      serviceVersion: options.Resource.ServiceVersion,
      autoGenerateServiceInstanceId: false);

    // Reserved attribute keys that should not be overridden from configuration
    var reservedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "service.name",
      "service.namespace",
      "service.instance.id",
      "service.version"
    };

    // Add custom attributes from configuration (skip reserved keys to avoid conflicts)
    foreach (var attr in options.Resource.Attributes.Where(a => !reservedKeys.Contains(a.Key)))
    {
      resource.AddAttributes(new[] {
        new KeyValuePair<string, object>(attr.Key, attr.Value)
      });
    }

    return resource;
  }

  private void ConfigureLogging(
    LoggerProviderBuilder builder,
    OpenTelemetryOptions options,
    string? otlpEndpoint)
  {
    // If developer provided override, use it exclusively
    if (loggingOverride != null)
    {
      loggingOverride(builder);
      return;
    }

    // Otherwise use configuration-driven approach
    if (options.Logging.EnableConsoleExporter)
    {
      builder.AddConsoleExporter();
    }

    if (ShouldEnableOtlpExporter(options.Logging.EnableOtlpExporter, otlpEndpoint))
    {
      builder.AddOtlpExporter(otlp =>
      {
        otlp.Endpoint = new Uri(otlpEndpoint!);
        otlp.Protocol = options.Otlp.Protocol;
        otlp.TimeoutMilliseconds = options.Otlp.TimeoutMilliseconds;

        if (options.Otlp.Headers.Count > 0)
        {
          otlp.Headers = string.Join(",", options.Otlp.Headers.Select(h => $"{h.Key}={h.Value}"));
        }
      });
    }
  }

  private void ConfigureTracing(
    TracerProviderBuilder builder,
    OpenTelemetryOptions options,
    string? otlpEndpoint)
  {
    // If developer provided override, use it exclusively
    if (tracingOverride != null)
    {
      tracingOverride(builder);
      return;
    }

    // Otherwise use configuration-driven approach
    if (options.Tracing.EnableAspNetCoreInstrumentation)
    {
      builder.AddAspNetCoreInstrumentation();
    }

    if (options.Tracing.EnableHttpClientInstrumentation)
    {
      builder.AddHttpClientInstrumentation();
    }

    if (ShouldEnableOtlpExporter(options.Tracing.EnableOtlpExporter, otlpEndpoint))
    {
      builder.AddOtlpExporter(otlp =>
      {
        otlp.Endpoint = new Uri(otlpEndpoint!);
        otlp.Protocol = options.Otlp.Protocol;
        otlp.TimeoutMilliseconds = options.Otlp.TimeoutMilliseconds;

        if (options.Otlp.Headers.Count > 0)
        {
          otlp.Headers = string.Join(",", options.Otlp.Headers.Select(h => $"{h.Key}={h.Value}"));
        }
      });
    }
  }

  private void ConfigureMetrics(
    MeterProviderBuilder builder,
    OpenTelemetryOptions options,
    string? otlpEndpoint)
  {
    // If developer provided override, use it exclusively
    if (metricsOverride != null)
    {
      metricsOverride(builder);
      return;
    }

    // Otherwise use configuration-driven approach
    if (options.Metrics.EnableAspNetCoreInstrumentation)
    {
      builder.AddAspNetCoreInstrumentation();
    }

    if (options.Metrics.EnableHttpClientInstrumentation)
    {
      builder.AddHttpClientInstrumentation();
    }

    if (options.Metrics.EnableRuntimeInstrumentation)
    {
      builder.AddRuntimeInstrumentation();
    }

    if (ShouldEnableOtlpExporter(options.Metrics.EnableOtlpExporter, otlpEndpoint))
    {
      builder.AddOtlpExporter(otlp =>
      {
        otlp.Endpoint = new Uri(otlpEndpoint!);
        otlp.Protocol = options.Otlp.Protocol;
        otlp.TimeoutMilliseconds = options.Otlp.TimeoutMilliseconds;

        if (options.Otlp.Headers.Count > 0)
        {
          otlp.Headers = string.Join(",", options.Otlp.Headers.Select(h => $"{h.Key}={h.Value}"));
        }
      });
    }
  }

  private static bool ShouldEnableOtlpExporter(bool explicitlyEnabled, string? endpoint)
  {
    // Enable OTLP exporter if:
    // 1. Explicitly enabled in configuration, OR
    // 2. Endpoint is configured (implicit enable)
    return explicitlyEnabled || !string.IsNullOrWhiteSpace(endpoint);
  }
}