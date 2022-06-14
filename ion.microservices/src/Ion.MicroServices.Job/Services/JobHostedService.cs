using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ion.MicroServices.Job.Services;

internal class JobHostedService : IHostedService
{
    private readonly IMicroService microservice;
    private readonly IHostApplicationLifetime lifetime;
    private readonly ILogger<JobHostedService> logger;

    public JobHostedService(IMicroService microservice, IHostApplicationLifetime lifetime, ILogger<JobHostedService> logger)
    {
        this.microservice = microservice ?? throw new ArgumentNullException(nameof(microservice));
        this.lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        lifetime.ApplicationStarted.Register(async () =>
        {
            var svc = (MicroService)microservice;
            var svcs = svc.Host.Services.GetServices<IHostedJobService>();

            try
            {
                if (svcs != null)
                {
                    logger.LogInformation($"Starting {svcs.Count()} IHostedJobService(s) ...");
                    await Task.WhenAll(svcs.Select(s => s.StartAsync(cancellationToken)));
                }

                await Task.Delay(1000);

                logger.LogInformation($"Job has finished executing all async IHostedJobService(s). Shutting down.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to execute IHostedJobService");
            }

            await Task.Delay(1000);

            lifetime.StopApplication();
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}