namespace Hive.Messaging.Middleware;

/// <summary>
/// Thrown when a message is received before the service is ready to process it.
/// </summary>
public sealed class ServiceNotReadyException : Exception
{
  /// <summary>
  /// Initializes a new instance of <see cref="ServiceNotReadyException"/>.
  /// </summary>
  /// <param name="serviceName">The name of the service that is not ready.</param>
  public ServiceNotReadyException(string serviceName)
    : base($"Service '{serviceName}' is not ready to process messages") { }
}