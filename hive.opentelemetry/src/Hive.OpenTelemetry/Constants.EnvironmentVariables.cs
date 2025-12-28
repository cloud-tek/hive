namespace Hive.OpenTelemetry;

/// <summary>
/// Constants associated with OpenTelemetry
/// </summary>
public static class Constants
{
  /// <summary>
  /// OpenTelemetry Environment Variables
  /// </summary>
  public static class Environment
  {
    /// <summary>
    /// Specifies the OTLP endpoint
    /// </summary>
    public const string OtelExporterOtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";
  }

  /// <summary>
  /// (optional) Logging exporter configuration section key
  /// </summary>
  public const string OtelLoggingExporterSection = "OpenTelemetry:Logging";

  /// <summary>
  /// (optional) Tracing exporter configuration section key
  /// </summary>
  public const string OtelTracingExporterSection = "OpenTelemetry:Tracing";

  /// <summary>
  /// (optional) Metrics exporter configuration section key
  /// </summary>
  public const string OtelMetricsExporterSection = "OpenTelemetry:Metrics";
}