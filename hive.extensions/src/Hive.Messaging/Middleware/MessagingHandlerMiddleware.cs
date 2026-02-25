using System.Diagnostics;
using Hive.Messaging.Telemetry;
using Wolverine;

namespace Hive.Messaging.Middleware;

/// <summary>
/// Wolverine middleware that records handler duration and error metrics.
/// </summary>
public class MessagingHandlerMiddleware
{
  private readonly Stopwatch _stopwatch = new();
  private TagList _tags;

  /// <summary>
  /// Called before the handler executes. Captures message metadata and starts timing.
  /// </summary>
  /// <param name="context">The Wolverine message context.</param>
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

  /// <summary>
  /// Called after the handler completes (success or failure). Records metrics.
  /// </summary>
  /// <param name="context">The Wolverine message context.</param>
  /// <param name="exception">The exception if the handler failed, or null on success.</param>
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
