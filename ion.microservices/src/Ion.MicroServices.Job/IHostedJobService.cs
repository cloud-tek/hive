namespace Ion.MicroServices.Job;

public interface IHostedJobService
{
    Task StartAsync(CancellationToken cancellationToken);
}