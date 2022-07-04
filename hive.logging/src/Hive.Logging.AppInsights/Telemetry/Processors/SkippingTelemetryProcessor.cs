using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace Hive.Logging.AppInsights.Telemetry.Processors;

public class SkippingTelemetryProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor next;
    private readonly IList<PathString> pathsToSkip;

    public SkippingTelemetryProcessor(ITelemetryProcessor next, IEnumerable<string> pathsToSkip)
    {
        this.next = next;
        this.pathsToSkip = pathsToSkip.Select(x => new PathString(x)).ToList();
    }
    
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
    
    private bool ShouldSkipDependency(DependencyTelemetry telemetry)
    {
        switch (telemetry.Type)
        {
            default:
                return false;
        }
    }
}