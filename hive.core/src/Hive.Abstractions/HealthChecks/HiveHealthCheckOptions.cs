namespace Hive.HealthChecks;

/// <summary>
/// Per-check configuration. Lives in Abstractions because it is referenced
/// by <see cref="IHiveHealthCheck.ConfigureDefaults"/>  (static abstract).
/// Only uses BCL types and <see cref="ReadinessThreshold"/> (also in Abstractions).
/// </summary>
public sealed class HiveHealthCheckOptions
{
  /// <summary>
  /// The evaluation interval for this check. When null, falls back to the global interval.
  /// </summary>
  public TimeSpan? Interval { get; set; }

  /// <summary>
  /// Whether this check affects the <c>IsReady</c> computation.
  /// </summary>
  public bool AffectsReadiness { get; set; } = true;

  /// <summary>
  /// Whether this check must pass during startup before the service reports readiness.
  /// </summary>
  public bool BlockReadinessProbeOnStartup { get; set; } = true;

  /// <summary>
  /// Minimum status for this check to be considered passing for readiness.
  /// Default: <see cref="ReadinessThreshold.Degraded"/> (both Healthy and Degraded pass).
  /// </summary>
  public ReadinessThreshold ReadinessThreshold { get; set; } = ReadinessThreshold.Degraded;

  /// <summary>
  /// Number of consecutive evaluations below <see cref="ReadinessThreshold"/> before
  /// this check actually affects IsReady. Default: 1 (immediate).
  /// </summary>
  public int FailureThreshold { get; set; } = 1;

  /// <summary>
  /// Number of consecutive passing evaluations required to restore
  /// IsPassingForReadiness after a failure. Default: 1 (instant recovery).
  /// </summary>
  public int SuccessThreshold { get; set; } = 1;

  /// <summary>
  /// Maximum time allowed for a single evaluation. When exceeded, the evaluation
  /// is cancelled and the check is marked Unhealthy. Default: 30 seconds.
  /// </summary>
  public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}