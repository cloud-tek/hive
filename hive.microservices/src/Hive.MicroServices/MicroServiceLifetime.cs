// ReSharper disable MemberCanBePrivate.Global
namespace Hive.MicroServices;

internal class MicroServiceLifetime : IMicroServiceLifetime
{
  private bool _disposed;

  public CancellationToken ServiceStarted => ServiceStartedTokenSource.Token;
  public CancellationTokenSource ServiceStartedTokenSource { get; } = new CancellationTokenSource();
  public CancellationToken StartupFailed => StartupFailedTokenSource.Token;
  internal CancellationTokenSource StartupFailedTokenSource { get; } = new CancellationTokenSource();

  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    ServiceStartedTokenSource?.Dispose();
    StartupFailedTokenSource?.Dispose();

    _disposed = true;
  }
}