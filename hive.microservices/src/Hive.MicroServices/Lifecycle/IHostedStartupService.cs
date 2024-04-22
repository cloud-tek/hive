namespace Hive.MicroServices.Lifecycle;

/// <summary>
/// An interface used to decorate IHostedServices which are required as service's startup
/// </summary>
public interface IHostedStartupService
{
  /// <summary>
  /// Indicates that the service has completed it's startup
  /// </summary>
  bool Completed { get; }

  /// <summary>
  /// Starts the IHostedStartupService
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
  Task StartAsync(CancellationToken cancellationToken);
}