namespace Hive.HealthChecks;

/// <summary>
/// The minimum <see cref="HealthCheckStatus"/> that counts as "passing" for readiness.
/// </summary>
public enum ReadinessThreshold
{
  /// <summary>
  /// Both Healthy and Degraded are considered passing.
  /// Only Unhealthy removes readiness. This is the default.
  /// </summary>
  Degraded,

  /// <summary>
  /// Only Healthy is considered passing.
  /// Both Degraded and Unhealthy remove readiness.
  /// </summary>
  Healthy
}