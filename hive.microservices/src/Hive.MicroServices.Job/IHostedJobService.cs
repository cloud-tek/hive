namespace Hive.MicroServices.Job;

/// <summary>
/// An interface used to decorate IHostedServices which are required as service's startup
/// </summary>
public interface IHostedJobService
{
  /// <summary>
  /// Starts the IHostedJobService
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns><see cref="Task"/></returns>
  Task StartAsync(CancellationToken cancellationToken);
}