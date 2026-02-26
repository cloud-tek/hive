using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Hive.Messaging.Telemetry;

internal sealed class WolverineSendActivityListener : IHostedService, IDisposable
{
  private const string TrackedTag = "hive.messaging.tracked";
  private readonly ActivityListener _listener;

  public WolverineSendActivityListener()
  {
    _listener = new ActivityListener
    {
      ShouldListenTo = source => source.Name == "Wolverine",
      ActivityStopped = OnActivityStopped,
      Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
    };
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    ActivitySource.AddActivityListener(_listener);
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _listener.Dispose();
    return Task.CompletedTask;
  }

  public void Dispose()
  {
    _listener.Dispose();
  }

  private static void OnActivityStopped(Activity activity)
  {
    if (activity.OperationName.StartsWith("send", StringComparison.OrdinalIgnoreCase) ||
        activity.OperationName.StartsWith("publish", StringComparison.OrdinalIgnoreCase))
    {
      var alreadyTracked = activity.GetTagItem(TrackedTag) is true;
      if (!alreadyTracked)
      {
        var tags = new TagList
        {
          { "messaging.message.type", activity.GetTagItem("messaging.message.type")?.ToString() ?? "unknown" }
        };

        MessagingMeter.MessagesSent.Add(1, tags);
      }
    }
  }
}