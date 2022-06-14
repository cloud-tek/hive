namespace Ion.MicroServices.Lifecycle;

public interface IHostedStartupService
{
    bool Completed { get; }

    Task StartAsync(CancellationToken cancellationToken);
}