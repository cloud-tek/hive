namespace Hive;

public interface IMicroServiceLifetime
{
    CancellationToken ServiceStarted { get; }
    CancellationToken StartupFailed { get; }
}
