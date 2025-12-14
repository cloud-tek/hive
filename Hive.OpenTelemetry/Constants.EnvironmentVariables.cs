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
}