namespace Hive.HealthChecks;

/// <summary>
/// Thread-safe registry that tracks per-check state. The readiness middleware
/// consults <see cref="IHealthCheckStateProvider.GetSnapshots"/> to determine
/// the HTTP status code for the readiness probe.
/// </summary>
internal sealed class HealthCheckRegistry : IHealthCheckStateProvider, IDisposable
{
  private readonly ReaderWriterLockSlim _lock = new();
  private readonly Dictionary<string, HealthCheckState> _states = new(StringComparer.OrdinalIgnoreCase);

  public void Register(string name, HiveHealthCheckOptions options)
  {
    _lock.EnterWriteLock();
    try
    {
      _states[name] = new HealthCheckState
      {
        Name = name,
        AffectsReadiness = options.AffectsReadiness,
        ReadinessThreshold = options.ReadinessThreshold,
        FailureThreshold = options.FailureThreshold,
        SuccessThreshold = options.SuccessThreshold
      };
    }
    finally
    {
      _lock.ExitWriteLock();
    }
  }

  public void UpdateAndRecompute(string name, HealthCheckStatus status, TimeSpan duration, string? error)
  {
    _lock.EnterWriteLock();
    try
    {
      if (!_states.TryGetValue(name, out var state))
        return;

      state.Status = status;
      state.LastCheckedAt = DateTimeOffset.UtcNow;
      state.Duration = duration;
      state.Error = error;

      if (IsPassing(status, state.ReadinessThreshold))
      {
        state.ConsecutiveSuccesses++;
        state.ConsecutiveFailures = 0;

        if (!state.IsPassingForReadiness)
          state.IsPassingForReadiness = state.ConsecutiveSuccesses >= state.SuccessThreshold;
      }
      else
      {
        state.ConsecutiveFailures++;
        state.ConsecutiveSuccesses = 0;
        state.IsPassingForReadiness = state.ConsecutiveFailures < state.FailureThreshold;
      }
    }
    finally
    {
      _lock.ExitWriteLock();
    }
  }

  public IReadOnlyList<HealthCheckStateSnapshot> GetSnapshots()
  {
    _lock.EnterReadLock();
    try
    {
      return _states.Values.Select(s => s.ToSnapshot()).ToList();
    }
    finally
    {
      _lock.ExitReadLock();
    }
  }

  public void Dispose()
  {
    _lock.Dispose();
  }

  private static bool IsPassing(HealthCheckStatus status, ReadinessThreshold threshold) => threshold switch
  {
    ReadinessThreshold.Degraded => status is HealthCheckStatus.Healthy or HealthCheckStatus.Degraded,
    ReadinessThreshold.Healthy => status is HealthCheckStatus.Healthy,
    _ => false
  };
}