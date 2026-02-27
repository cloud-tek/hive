namespace Hive.HealthChecks;

/// <summary>
/// Lightweight signal that the <see cref="HealthCheckStartupService"/> sets on completion,
/// allowing <see cref="HealthCheckBackgroundService"/> to wait before starting evaluation loops.
/// </summary>
internal sealed class HealthCheckStartupGate
{
  private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

  /// <summary>
  /// Blocks until <see cref="Signal"/> is called or the token is cancelled.
  /// </summary>
  public Task WaitAsync(CancellationToken ct) => _tcs.Task.WaitAsync(ct);

  /// <summary>
  /// Signals that startup evaluation has completed.
  /// </summary>
  public void Signal() => _tcs.TrySetResult();
}