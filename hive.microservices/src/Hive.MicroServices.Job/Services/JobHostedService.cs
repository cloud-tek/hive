#pragma warning disable AsyncFixer03
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hive.MicroServices.Job.Services;

internal sealed class JobHostedService : IHostedService
{
  private readonly IMicroService microservice;
  private readonly IHostApplicationLifetime lifetime;
  private readonly ILogger<JobHostedService> logger;

  public JobHostedService(
    IMicroService microservice,
    IHostApplicationLifetime lifetime,
    ILogger<JobHostedService> logger)
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
        if (svcs == null || svcs.Any() == false)
        {
          throw new InvalidOperationException("At least one IHostedJobService needs to be registered");
        }

        logger.LogInformationJobHostedServiceStarted(svcs.Count());

        var tasks = svcs.Select(s => s.StartAsync(cancellationToken));

        await Task.WhenAll(tasks);
        await Task.Delay(1000);

        if (tasks.Any(t => t.Status == TaskStatus.Faulted))
        {
          throw new JobException("Job has finished executing all async IHostedJobService(s). At least 1 job failed to complete");
        }

        logger.LogInformationJobHostedServiceStopping();
      }
      catch (InvalidOperationException ioex)
      {
        logger.LogCriticalJobHostedServiceStartupFailure(ioex);
        throw;
      }
      catch (JobException jex)
      {
        logger.LogErrorJobHostedServiceError(jex);
        throw;
      }
      catch (Exception ex)
      {
        logger.LogCriticalJobHostedServiceUnhandledException(ex);
        throw;
      }
      finally
      {
        await Task.Delay(1000);

        lifetime.StopApplication();
      }
    });

    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }
}
#pragma warning restore AsyncFixer03

internal static partial class JobHostedServiceLogMessages
{
  [LoggerMessage((int)MicroServiceLogEventId.JobHostedServiceStarted, LogLevel.Information, "Starting {count} IHostedJobService(s) ...")]
  internal static partial void LogInformationJobHostedServiceStarted(this ILogger logger, int count);

  [LoggerMessage((int)MicroServiceLogEventId.JobHostedServiceStopping, LogLevel.Information, "IHostedJobService has finished executing all async IHostedJobService(s). Shutting down.")]
  internal static partial void LogInformationJobHostedServiceStopping(this ILogger logger);

  [LoggerMessage((int)MicroServiceLogEventId.JobHostedServiceCriticalFailure, LogLevel.Critical, "IHostedJobService failed to start")]
  internal static partial void LogCriticalJobHostedServiceStartupFailure(this ILogger logger, Exception exception);

  [LoggerMessage((int)MicroServiceLogEventId.UnhandledException, LogLevel.Critical, "IHostedJobService failed due to an unhandled exception")]
  internal static partial void LogCriticalJobHostedServiceUnhandledException(this ILogger logger, Exception exception);

  [LoggerMessage((int)MicroServiceLogEventId.JobHostedServiceErrorFailed, LogLevel.Error, "IHostedJobService failed to complete")]
  internal static partial void LogErrorJobHostedServiceError(this ILogger logger, Exception exception);
}