namespace Ion.MicroServices;

internal class MicroServiceLifetime : IMicroServiceLifetime
{
    public CancellationToken ServiceStarted => ServiceStartedTokenSource.Token;
    public CancellationTokenSource ServiceStartedTokenSource { get; } = new CancellationTokenSource();
    public CancellationToken StartupFailed => StartupFailedTokenSource.Token;
    internal CancellationTokenSource StartupFailedTokenSource { get; } = new CancellationTokenSource();
}