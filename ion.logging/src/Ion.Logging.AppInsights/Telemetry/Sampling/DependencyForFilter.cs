using Microsoft.ApplicationInsights.DataContracts;

namespace Ion.Logging.AppInsights.Telemetry.Sampling;

[Serializable]
internal struct DependencyForFilter
{
    public string CloudRoleName { get; set; }
    public string Type { get; set; }
    public string Target { get; }
    public string Name { get; }
    public double Duration { get; }
    public bool? Success { get; }
    public string ResultCode { get; }

    public DependencyForFilter(DependencyTelemetry dependencyTelemetry)
    {
        Type = dependencyTelemetry.Type ?? "";
        Target = dependencyTelemetry.Target ?? "";
        Name = dependencyTelemetry.Name ?? "";
        Duration = dependencyTelemetry.Duration.TotalMilliseconds;
        Success = dependencyTelemetry.Success;
        CloudRoleName = dependencyTelemetry.Context?.Cloud?.RoleName ?? "";
        ResultCode = dependencyTelemetry.ResultCode ?? "";
        
        // Custom = dependencyTelemetry.Metrics.ContainsKey("Custom")
        //     ? dependencyTelemetry.Metrics["Custom"]
        //     : (double?) null;
    }
}