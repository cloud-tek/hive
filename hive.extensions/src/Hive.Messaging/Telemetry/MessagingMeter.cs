using System.Diagnostics.Metrics;

namespace Hive.Messaging.Telemetry;

internal static class MessagingMeter
{
  private static readonly Meter Meter = new("Hive.Messaging");

  public static readonly Histogram<double> HandlerDuration =
    Meter.CreateHistogram<double>(
      "hive.messaging.handler.duration",
      unit: "ms",
      description: "Message handler execution duration in milliseconds");

  public static readonly Counter<long> HandlerCount =
    Meter.CreateCounter<long>(
      "hive.messaging.handler.count",
      description: "Total messages handled");

  public static readonly Counter<long> HandlerErrors =
    Meter.CreateCounter<long>(
      "hive.messaging.handler.errors",
      description: "Failed message handler executions");

  public static readonly Counter<long> MessagesSent =
    Meter.CreateCounter<long>(
      "hive.messaging.send.count",
      description: "Total messages sent/published");

  public static readonly Counter<long> SendErrors =
    Meter.CreateCounter<long>(
      "hive.messaging.send.errors",
      description: "Failed message send attempts");

  public static readonly Counter<long> GateNacked =
    Meter.CreateCounter<long>(
      "hive.messaging.gate.nacked",
      description: "Messages nacked by ReadinessMiddleware (IsReady == false)");
}