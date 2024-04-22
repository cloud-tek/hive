using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace Hive.Logging.AppInsights.Telemetry.Processors;

/// <summary>
/// A telemetry processor that skips telemetry based on the request path.
/// </summary>
public class SkippingTelemetryProcessor : ITelemetryProcessor
{
  private readonly ITelemetryProcessor next;
  private readonly IList<PathString> pathsToSkip;

  /// <summary>
  /// Initializes a new instance of the <see cref="SkippingTelemetryProcessor"/> class.
  /// </summary>
  /// <param name="next"></param>
  /// <param name="pathsToSkip"></param>
  public SkippingTelemetryProcessor(ITelemetryProcessor next, IEnumerable<string> pathsToSkip)
  {
    this.next = next;
    this.pathsToSkip = pathsToSkip.Select(x => new PathString(x)).ToList();
  }

  /// <summary>
  /// Processes the telemetry.
  /// </summary>
  /// <param name="item"></param>
  public void Process(ITelemetry item)
  {
    switch (item)
    {
      case RequestTelemetry request when ShouldSkipRequest(request): return;
      case DependencyTelemetry dependency when ShouldSkipDependency(dependency): return;
      default:
        next.Process(item);
        return;
    }
  }

  private bool ShouldSkipRequest(RequestTelemetry telemetry)
  {
    if (telemetry.Url != null)
    {
      var path = new PathString(telemetry.Url.AbsolutePath);
      return pathsToSkip.Any(x => path.StartsWithSegments(x));
    }

    return telemetry.Name == "Process";
  }

  private static bool ShouldSkipDependency(DependencyTelemetry telemetry)
  {
    switch (telemetry.Type)
    {
      default:
        return false;
    }
  }
}