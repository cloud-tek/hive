using System.Diagnostics;
using Hive.Messaging.Telemetry;
using Wolverine;

namespace Hive.Messaging.Middleware;

public class MessagingHandlerMiddleware
{
  private readonly Stopwatch _stopwatch = new();
  private TagList _tags;

  public void Before(IMessageContext context)
  {
    var envelope = context.Envelope;
    _tags = new TagList
    {
      { "messaging.message.type", envelope?.MessageType ?? "unknown" },
      { "messaging.source", envelope?.Destination?.ToString() ?? "unknown" }
    };

    _stopwatch.Start();
  }

  public void Finally(IMessageContext context, Exception? exception)
  {
    _stopwatch.Stop();
    MessagingMeter.HandlerDuration.Record(_stopwatch.Elapsed.TotalMilliseconds, _tags);

    if (exception is not null)
    {
      var errorTags = _tags;
      errorTags.Add("error.type", exception.GetType().Name);
      MessagingMeter.HandlerErrors.Add(1, errorTags);
    }
    else
    {
      MessagingMeter.HandlerCount.Add(1, _tags);
    }
  }
}
