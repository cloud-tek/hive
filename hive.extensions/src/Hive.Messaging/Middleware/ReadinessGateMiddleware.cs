using Hive.Messaging.Telemetry;

namespace Hive.Messaging.Middleware;

public static class ReadinessGateMiddleware
{
  public static void Before(IMicroService microService)
  {
    if (!microService.IsReady)
    {
      MessagingMeter.GateNacked.Add(1);
      throw new ServiceNotReadyException(microService.Name);
    }
  }
}
