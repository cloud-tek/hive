namespace Hive;

/// <summary>
/// Interface for microservice lifetime events
/// </summary>
public interface IMicroServiceLifetime : IDisposable
{
  /// <summary>
  /// One-time event indicating if the service has started
  /// </summary>
  CancellationToken ServiceStarted { get; }

  /// <summary>
  /// One-time event indicating if the service has failed to start
  /// </summary>
  CancellationToken StartupFailed { get; }
}