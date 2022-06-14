using Ion.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ion.MicroServices.Lifecycle;

public class ShutdownService : IHostedService
{
    private const int DefaultTimeoutSeconds = 30;
    private readonly IHostApplicationLifetime lifetime;
    private readonly ILogger<ShutdownService> logger;
    private readonly IActiveRequestsService service;

    public ShutdownService(IActiveRequestsService service, IHostApplicationLifetime lifetime, ILogger<ShutdownService> logger)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        this.lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        lifetime.ApplicationStopping.Register(async () => await ExecuteGracefulShutdown(DefaultTimeoutSeconds).ConfigureAwait(false));
        lifetime.ApplicationStopped.Register(async () =>
        {
            logger.LogInformation("MicroService stopped");

            // Ensure logs are flushed
            await Task.Delay(1.Seconds());
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task ExecuteGracefulShutdown(int timeout)
    {
        if (await TaskEx.TryWaitUntil(() => !service.HasActiveRequests,
            onFailure: () =>
            {
                logger.LogDebug($"MicroService has {service.Counter} active requests remaining");
            },
            frequency: 25.Milliseconds(),
            timeout: timeout.Seconds()).ConfigureAwait(false))
        {
            logger.LogInformation("MicroService drained successfully");
        }
        else
        {
            logger.LogError($"Failed to drain service within {timeout} [s]");
        }
    }
}