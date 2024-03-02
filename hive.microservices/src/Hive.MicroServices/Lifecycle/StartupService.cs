using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hive.MicroServices.Lifecycle;

/// <summary>
/// The hosted service which controls the startup of the microservice.
/// Startup will complete once all <see cref="IHostedStartupService"/> services have completed.
/// </summary>
public class StartupService : IHostedService
{
  private readonly IHostApplicationLifetime lifetime;
  private readonly ILogger<StartupService> logger;
  private readonly IMicroService service;

  /// <summary>
  /// Initializes a new instance of the <see cref="StartupService"/> class.
  /// </summary>
  /// <param name="service"></param>
  /// <param name="lifetime"></param>
  /// <param name="logger"></param>
  public StartupService(IMicroService service, IHostApplicationLifetime lifetime, ILogger<StartupService> logger)
  {
    this.service = service ?? throw new ArgumentNullException(nameof(service));
    this.lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <summary>
  /// Start the hosted service
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns><see cref="Task"/></returns>
  public Task StartAsync(CancellationToken cancellationToken)
  {
#pragma warning disable AsyncFixer03
    lifetime.ApplicationStarted.Register(async () => await ExecuteHostedStartupServices().ConfigureAwait(false));

    return Task.FromResult(0);
#pragma warning restore AsyncFixer03
  }

  /// <summary>
  /// Stop the hosted service
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns><see cref="Task"/> </returns>
  public Task StopAsync(CancellationToken cancellationToken)
  {
    return Task.FromResult(0);
  }

  private async Task ExecuteHostedStartupServices()
  {
    var svc = (MicroService)service;
    var svcs = svc.Host.Services.GetServices<IHostedStartupService>();

    try
    {
      if (svcs != null)
      {
        foreach (var s in svcs)
        {
          await s.StartAsync(default).ConfigureAwait(false);
        }
      }

      logger.LogInformationServiceStarted();

      svc.IsStarted = true;
      svc.IsReady = true;
    }
    catch (Exception ex)
    {
      logger.LogCriticalServiceFailedToStart(ex);
      ((MicroServiceLifetime)svc.Lifetime).StartupFailedTokenSource.Cancel();

      Environment.ExitCode = -1;
      lifetime.StopApplication();
    }
  }
}

internal static partial class StartupServiceLogMessages
{
  [LoggerMessage((int)MicroServiceLogEventId.ServiceStarted, LogLevel.Information, "Service started successfully")]
  internal static partial void LogInformationServiceStarted(this ILogger logger);

  [LoggerMessage((int)MicroServiceLogEventId.ServiceStartupCriticalFailure, LogLevel.Critical, "Service failed to start")]
  internal static partial void LogCriticalServiceFailedToStart(this ILogger logger, Exception exception);
}