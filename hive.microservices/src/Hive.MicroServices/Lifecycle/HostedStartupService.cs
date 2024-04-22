using Microsoft.Extensions.Logging;

namespace Hive.MicroServices.Lifecycle;

/// <summary>
/// HostedStartupServices are used to control service initialization.
/// The service will not report Started==true until all hosted services has completed.
/// </summary>
/// <typeparam name="T"><see cref="IHostedStartupService"/></typeparam>
public abstract class HostedStartupService<T> : IHostedStartupService
        where T : class, IHostedStartupService
{
  /// <summary>
  /// The <see cref="IHostedStartupService"/> logger
  /// </summary>
  protected readonly ILogger<IHostedStartupService> Logger = default!;

  /// <summary>
  /// Executed on startup. Provides a way to implement per-service startup logic.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
  protected abstract Task OnStartAsync(CancellationToken cancellationToken);

  /// <summary>
  /// Initializes a new instance of the <see cref="HostedStartupService{T}"/> class.
  /// </summary>
  /// <param name="loggerFactory"></param>
  protected HostedStartupService(ILoggerFactory loggerFactory)
  {
    Logger = loggerFactory?.CreateLogger<IHostedStartupService>() ??
             throw new ArgumentNullException(nameof(loggerFactory));
  }

  /// <summary>
  /// Start the hosted service
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns><see cref="Task"/> </returns>
  public async Task StartAsync(CancellationToken cancellationToken)
  {
    await OnStartAsync(cancellationToken);

    Completed = true;

    Logger.LogInformationHostedStartupServiceCompleted(typeof(T).Name);
  }

  /// <summary>
  /// Stops the <see cref="IHostedStartupService"/>. Executed during shutdown
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
  public Task StopAsync(CancellationToken cancellationToken)
  {
    return Task.FromResult(0);
  }

  /// <summary>
  /// Flagi indicating that the service has completed it's startup
  /// </summary>
  public bool Completed { get; private set; }
}

internal static partial class HostedStartupServiceLogExtensions
{
  [LoggerMessage((int)MicroServiceLogEventId.HostedStartupServiceCompleted, LogLevel.Information, "HostedStartupService<{name}> completed")]
  internal static partial void LogInformationHostedStartupServiceCompleted(this ILogger logger, string name);
}