namespace Hive;

public static partial class Constants
{
  /// <summary>
  /// Constants related to HTTP Headers
  /// </summary>
  public static class Headers
  {
    /// <summary>
    /// The Request-Id header
    /// </summary>
    public const string RequestId = "Request-Id";

    /// <summary>
    /// The TraceParentId header for OpenTelemetry
    /// </summary>
    public const string TraceParentId = "traceparent";
  }
}