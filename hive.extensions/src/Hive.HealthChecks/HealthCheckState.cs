namespace Hive.HealthChecks;

/// <summary>
/// Mutable per-check state, maintained by <see cref="HealthCheckRegistry"/>.
/// Internal to the health check runtime.
/// </summary>
internal sealed class HealthCheckState
{
  public required string Name { get; init; }
  public HealthCheckStatus Status { get; set; } = HealthCheckStatus.Unknown;
  public DateTimeOffset? LastCheckedAt { get; set; }
  public TimeSpan? Duration { get; set; }
  public string? Error { get; set; }
  public required bool AffectsReadiness { get; init; }
  public required ReadinessThreshold ReadinessThreshold { get; init; }
  public required int FailureThreshold { get; init; }
  public required int SuccessThreshold { get; init; }
  public int ConsecutiveFailures { get; set; }
  public int ConsecutiveSuccesses { get; set; }
  public bool IsPassingForReadiness { get; set; } = true;

  public HealthCheckStateSnapshot ToSnapshot() => new(
    Name,
    Status,
    LastCheckedAt,
    Duration,
    Error,
    AffectsReadiness,
    ReadinessThreshold,
    ConsecutiveFailures,
    ConsecutiveSuccesses,
    IsPassingForReadiness);
}
