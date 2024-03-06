using Hive.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hive.MicroServices.Lifecycle;

/// <summary>
/// Service to ensure that the microservice is drained before stopping
/// </summary>
public class ShutdownService : IHostedService
{
  private const int DefaultTimeoutSeconds = 30;
  private readonly IHostApplicationLifetime _lifetime;
  private readonly ILogger<ShutdownService> _logger;
  private readonly IActiveRequestsService _service;

  /// <summary>
  /// Create a new instance of the service
  /// </summary>
  /// <param name="service"></param>
  /// <param name="lifetime"></param>
  /// <param name="logger"></param>
  /// <exception cref="ArgumentNullException">Thrown when any of the provided arguments are null</exception>
  public ShutdownService(IActiveRequestsService service, IHostApplicationLifetime lifetime, ILogger<ShutdownService> logger)
  {
    _service = service ?? throw new ArgumentNullException(nameof(service));
    _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <summary>
  /// Start the hosted service
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns><see cref="Task"/></returns>
  public Task StartAsync(CancellationToken cancellationToken)
  {
#pragma warning disable AsyncFixer03
    _lifetime.ApplicationStopping.Register(async () => await ExecuteGracefulShutdown(DefaultTimeoutSeconds).ConfigureAwait(false));
    _lifetime.ApplicationStopped.Register(async () =>
    {
      _logger.LogInformationServiceStopping();

      // Ensure logs are flushed
      await Task.Delay(1.Seconds());
    });
#pragma warning restore AsyncFixer03
    return Task.CompletedTask;
  }

  /// <summary>
  /// Stops the hosted service
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns><see cref="Task"/></returns>
  public Task StopAsync(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }

  private async Task ExecuteGracefulShutdown(int timeout)
  {
    if (await TaskEx.TryWaitUntil(
          condition: () => !_service.HasActiveRequests,
          onFailure: () =>
          {
            _logger.LogDebugActiveRequestsToBeDrained(_service.Counter);
          },
          frequency: 25.Milliseconds(),
          timeout: timeout.Seconds()).ConfigureAwait(false))
    {
      _logger.LogInformationServiceDrained();
    }
    else
    {
      _logger.LogErrroServiceDrainedFailed(timeout);
    }
  }
}

internal static partial class ShutdownServiceLogMessages
{
  [LoggerMessage((int)MicroServiceLogEventId.ServiceStopping, LogLevel.Information, "MicroService is stopping")]
  internal static partial void LogInformationServiceStopping(this ILogger logger);

  [LoggerMessage((int)MicroServiceLogEventId.ServiceDrainedHTTP, LogLevel.Information, "MicroService drained successfully")]
  internal static partial void LogInformationServiceDrained(this ILogger logger);

  [LoggerMessage((int)MicroServiceLogEventId.ServiceDrainedFailedHTTP, LogLevel.Information, "Failed to drain service within {timeout} [s]")]
  internal static partial void LogErrroServiceDrainedFailed(this ILogger logger, int timeout);

  [LoggerMessage((int)MicroServiceLogEventId.ServiceDrainRemainingHTTP, LogLevel.Debug, "MicroService has {counter} active requests remaining")]
  internal static partial void LogDebugActiveRequestsToBeDrained(this ILogger logger, long counter);
}