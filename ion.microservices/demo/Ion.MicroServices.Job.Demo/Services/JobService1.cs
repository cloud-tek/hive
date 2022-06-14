using Ion.Extensions;

namespace Ion.MicroServices.Job.Demo.Services;

public class JobService1 : IHostedJobService
{
    private readonly ILogger<JobService1> logger;

    public JobService1(ILogger<JobService1> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var rand = new Random(DateTime.UtcNow.Millisecond);

        return TaskEx.TryWaitUntil(() => true == false,
            500.Milliseconds(),
            rand.Next(3000, 10000).Milliseconds(),
            () =>
            {
                logger.LogInformation($"{nameof(JobService1)} doing work ...");
            });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}