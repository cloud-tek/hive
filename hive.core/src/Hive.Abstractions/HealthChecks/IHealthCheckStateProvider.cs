namespace Hive.HealthChecks;

/// <summary>
/// Provides read-only health check state snapshots for the readiness probe.
/// Resolved optionally by ReadinessMiddleware from DI.
/// </summary>
public interface IHealthCheckStateProvider
{
  /// <summary>
  /// Returns a consistent snapshot of all registered health check states.
  /// </summary>
  IReadOnlyList<HealthCheckStateSnapshot> GetSnapshots();
}