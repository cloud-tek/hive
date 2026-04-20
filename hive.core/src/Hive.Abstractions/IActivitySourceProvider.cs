namespace Hive;

/// <summary>
/// Implemented by extensions that expose ActivitySource names for OpenTelemetry tracing.
/// The OpenTelemetry extension auto-discovers these and subscribes to them.
/// </summary>
public interface IActivitySourceProvider
{
  /// <summary>
  /// The ActivitySource names to subscribe to for distributed tracing
  /// </summary>
  IEnumerable<string> ActivitySourceNames { get; }
}