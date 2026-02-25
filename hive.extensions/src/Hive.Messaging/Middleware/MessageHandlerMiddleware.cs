using System.Diagnostics;
using Hive.Messaging.Telemetry;
using Wolverine;

namespace Hive.Messaging.Middleware;

/// <summary>
/// Wolverine middleware that records handler duration and error metrics.
/// State is passed from <see cref="Before"/> to <see cref="Finally"/> via a
/// <see cref="MessageTelemetryContext"/> value returned by Wolverine's code-generated pipeline.
/// </summary>
public static class MessageHandlerMiddleware
{
  /// <summary>
  /// Per-message telemetry state passed through the Wolverine handler pipeline.
  /// </summary>
  public record struct MessageTelemetryContext(Stopwatch Stopwatch, TagList Tags);

  /// <summary>
  /// Called before the handler executes. Captures message metadata and starts timing.
  /// </summary>
  /// <param name="context">The Wolverine message context.</param>
  /// <returns>Telemetry state to be forwarded to <see cref="Finally"/>.</returns>
  public static MessageTelemetryContext Before(IMessageContext context)
  {
    var envelope = context.Envelope;
    var tags = new TagList
    {
      { "messaging.message.type", envelope?.MessageType ?? "unknown" },
      { "messaging.source", envelope?.Destination?.ToString() ?? "unknown" }
    };

    return new MessageTelemetryContext(Stopwatch.StartNew(), tags);
  }

  /// <summary>
  /// Called after the handler completes (success or failure). Records metrics.
  /// </summary>
  /// <param name="messageContext">Telemetry state captured by <see cref="Before"/>.</param>
  /// <param name="exception">The exception if the handler failed, or null on success.</param>
  public static void Finally(MessageTelemetryContext messageContext, Exception? exception)
  {
    messageContext.Stopwatch.Stop();
    MessagingMeter.HandlerDuration.Record(
      messageContext.Stopwatch.Elapsed.TotalMilliseconds, messageContext.Tags);

    if (exception is not null)
    {
      var errorTags = messageContext.Tags;
      errorTags.Add("error.type", exception.GetType().Name);
      MessagingMeter.HandlerErrors.Add(1, errorTags);
    }
    else
    {
      MessagingMeter.HandlerCount.Add(1, messageContext.Tags);
    }
  }
}