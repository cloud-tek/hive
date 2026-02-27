namespace Hive.HealthChecks;

/// <summary>
/// The status of a health check evaluation.
/// </summary>
public enum HealthCheckStatus
{
  /// <summary>
  /// The health check has not yet been evaluated.
  /// </summary>
  Unknown,

  /// <summary>
  /// The dependency is healthy.
  /// </summary>
  Healthy,

  /// <summary>
  /// The dependency is operational but suboptimal.
  /// </summary>
  Degraded,

  /// <summary>
  /// The dependency is down.
  /// </summary>
  Unhealthy
}