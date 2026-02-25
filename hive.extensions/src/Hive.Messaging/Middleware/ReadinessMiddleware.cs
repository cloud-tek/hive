using Hive.Messaging.Telemetry;

namespace Hive.Messaging.Middleware;

/// <summary>
/// Wolverine middleware that rejects messages when the service is not ready.
/// Only registered by <see cref="MessagingExtension"/> (full handling + sending),
/// which requires <see cref="IMicroService"/>. Not used by <c>MessagingSendExtension</c>
/// (send-only for <see cref="IMicroServiceCore"/> hosts such as Azure Functions).
/// </summary>
public static class ReadinessMiddleware
{
  /// <summary>
  /// Checks the service readiness state before allowing message processing.
  /// Requires <see cref="IMicroService"/> (not <see cref="IMicroServiceCore"/>)
  /// because the <see cref="IMicroService.IsReady"/> property is only available on ASP.NET-based hosts.
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