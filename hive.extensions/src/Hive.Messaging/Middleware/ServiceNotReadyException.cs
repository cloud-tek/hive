namespace Hive.Messaging.Middleware;

public sealed class ServiceNotReadyException : Exception
{
  public ServiceNotReadyException(string serviceName)
    : base($"Service '{serviceName}' is not ready to process messages") { }
}
