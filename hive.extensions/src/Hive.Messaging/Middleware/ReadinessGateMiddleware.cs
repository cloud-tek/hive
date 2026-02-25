using Hive.Messaging.Telemetry;

namespace Hive.Messaging.Middleware;

/// <summary>
/// Wolverine middleware that rejects messages when the service is not ready.
/// </summary>
public static class ReadinessGateMiddleware
{
  /// <summary>
  /// Checks the service readiness state before allowing message processing.
  /// </summary>
  /// <param name="microService">The microservice instance to check readiness on.</param>
  /// <exception cref="ServiceNotReadyException">Thrown when the service is not ready.</exception>
  public static void Before(IMicroService microService)
  {
    if (!microService.IsReady)
    {
      MessagingMeter.GateNacked.Add(1);
      throw new ServiceNotReadyException(microService.Name);
    }
  }
}
