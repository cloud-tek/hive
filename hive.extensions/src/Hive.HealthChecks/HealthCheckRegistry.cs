namespace Hive.HealthChecks;

/// <summary>
/// Thread-safe registry that tracks per-check state. The readiness middleware
/// consults <see cref="IHealthCheckStateProvider.GetSnapshots"/> to determine
/// the HTTP status code for the readiness probe.
/// </summary>
internal sealed class HealthCheckRegistry : IHealthCheckStateProvider
{
  private readonly object _sync = new();
  private readonly Dictionary<string, HealthCheckState> _states = new(StringComparer.OrdinalIgnoreCase);

  public void Register(string name, HiveHealthCheckOptions options)
  {
    lock (_sync)
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
  }

  public void UpdateAndRecompute(string name, HealthCheckStatus status, TimeSpan duration, string? error)
  {
    lock (_sync)
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
  }

  public IReadOnlyList<HealthCheckStateSnapshot> GetSnapshots()
  {
    lock (_sync)
    {
      return _states.Values.Select(s => s.ToSnapshot()).ToList();
    }
  }

  private static bool IsPassing(HealthCheckStatus status, ReadinessThreshold threshold) => threshold switch
  {
    ReadinessThreshold.Degraded => status is HealthCheckStatus.Healthy or HealthCheckStatus.Degraded,
    ReadinessThreshold.Healthy => status is HealthCheckStatus.Healthy,
    _ => false
  };
}
