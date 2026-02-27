namespace Hive.HealthChecks;

/// <summary>
/// Read-only snapshot of a single health check's state, used for probe responses.
/// </summary>
public sealed record HealthCheckStateSnapshot(
  string Name,
  HealthCheckStatus Status,
  DateTimeOffset? LastCheckedAt,
  TimeSpan? Duration,
  string? Error,
  bool AffectsReadiness,
  ReadinessThreshold ReadinessThreshold,
  int ConsecutiveFailures,
  int ConsecutiveSuccesses,
  bool IsPassingForReadiness);