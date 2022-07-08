using Microsoft.Extensions.Logging;

namespace Hive.MicroServices.Lifecycle;

public abstract class HostedStartupService<T> : IHostedStartupService
        where T : class, IHostedStartupService
{
    protected abstract Task OnStartAsync(CancellationToken cancellationToken);

    protected readonly ILogger<IHostedStartupService> logger;

    protected HostedStartupService(ILoggerFactory loggerFactory)
    {
        logger = loggerFactory?.CreateLogger<IHostedStartupService>() ??
                 throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await OnStartAsync(cancellationToken);

        Completed = true;

        logger.LogInformation($"{typeof(T).Name} completed");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(0);
    }

    public bool Completed { get; private set; }
}
