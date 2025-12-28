using OpenTelemetry.Exporter;

namespace Hive.OpenTelemetry;

/// <summary>
/// OpenTelemetry configuration options
/// </summary>
public class OpenTelemetryOptions
{
  /// <summary>
  /// Configuration section key
  /// </summary>
  public const string SectionKey = "OpenTelemetry";

  /// <summary>
  /// Resource attributes configuration
  /// </summary>
  public ResourceOptions Resource { get; set; } = new();

  /// <summary>
  /// Logging configuration
  /// </summary>
  public LoggingOptions Logging { get; set; } = new();

  /// <summary>
  /// Tracing configuration
  /// </summary>
  public TracingOptions Tracing { get; set; } = new();

  /// <summary>
  /// Metrics configuration
  /// </summary>
  public MetricsOptions Metrics { get; set; } = new();

  /// <summary>
  /// OTLP exporter configuration
  /// </summary>
  public OtlpOptions Otlp { get; set; } = new();
}

/// <summary>
/// Resource attributes configuration
/// </summary>
public class ResourceOptions
{
  /// <summary>
  /// Service namespace (optional)
  /// </summary>
  public string? ServiceNamespace { get; set; }

  /// <summary>
  /// Service version (optional)
  /// </summary>
  public string? ServiceVersion { get; set; }

  /// <summary>
  /// Additional resource attributes
  /// </summary>
  public Dictionary<string, string> Attributes { get; set; } = new();
}

/// <summary>
/// Logging configuration options
/// </summary>
public class LoggingOptions
{
  /// <summary>
  /// Enable console exporter (default: true)
  /// </summary>
  public bool EnableConsoleExporter { get; set; } = true;

  /// <summary>
  /// Enable OTLP exporter (default: false, enabled if Otlp.Endpoint is set)
  /// </summary>
  public bool EnableOtlpExporter { get; set; }
}

/// <summary>
/// Tracing configuration options
/// </summary>
public class TracingOptions
{
  /// <summary>
  /// Enable ASP.NET Core instrumentation (default: true)
  /// </summary>
  public bool EnableAspNetCoreInstrumentation { get; set; } = true;

  /// <summary>
  /// Enable HTTP Client instrumentation (default: true)
  /// </summary>
  public bool EnableHttpClientInstrumentation { get; set; } = true;

  /// <summary>
  /// Enable OTLP exporter (default: false, enabled if Otlp.Endpoint is set)
  /// </summary>
  public bool EnableOtlpExporter { get; set; }
}

/// <summary>
/// Metrics configuration options
/// </summary>
public class MetricsOptions
{
  /// <summary>
  /// Enable ASP.NET Core instrumentation (default: true)
  /// </summary>
  public bool EnableAspNetCoreInstrumentation { get; set; } = true;

  /// <summary>
  /// Enable HTTP Client instrumentation (default: true)
  /// </summary>
  public bool EnableHttpClientInstrumentation { get; set; } = true;

  /// <summary>
  /// Enable Runtime instrumentation (default: true)
  /// </summary>
  public bool EnableRuntimeInstrumentation { get; set; } = true;

  /// <summary>
  /// Enable OTLP exporter (default: false, enabled if Otlp.Endpoint is set)
  /// </summary>
  public bool EnableOtlpExporter { get; set; }
}

/// <summary>
/// OTLP exporter configuration options
/// </summary>
public class OtlpOptions
{
  /// <summary>
  /// OTLP endpoint (e.g., "http://localhost:4317")
  /// Falls back to OTEL_EXPORTER_OTLP_ENDPOINT environment variable
  /// </summary>
  public string? Endpoint { get; set; }

  /// <summary>
  /// Protocol (default: Grpc)
  /// </summary>
  public OtlpExportProtocol Protocol { get; set; } = OtlpExportProtocol.Grpc;

  /// <summary>
  /// Headers (e.g., for authentication)
  /// </summary>
  public Dictionary<string, string> Headers { get; set; } = new();

  /// <summary>
  /// Timeout in milliseconds (default: 10000)
  /// Valid range: 1000-60000
  /// </summary>
  public int TimeoutMilliseconds { get; set; } = 10000;
}
