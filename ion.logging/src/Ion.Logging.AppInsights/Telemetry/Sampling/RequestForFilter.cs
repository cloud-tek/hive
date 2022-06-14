using Microsoft.ApplicationInsights.DataContracts;

namespace Ion.Logging.AppInsights.Telemetry.Sampling;

[Serializable]
internal struct RequestForFilter
{
    public string CloudRoleName { get; }
    public string Name { get; }
    public double Duration { get; }
    public bool? Success { get; }
    public string ResponseCode { get; set; }
    public string Url { get; }

    public RequestForFilter(RequestTelemetry requestTelemetry)
    {
        Name = requestTelemetry.Name ?? "";
        Duration = requestTelemetry.Duration.TotalMilliseconds;
        Success = requestTelemetry.Success;
        CloudRoleName = requestTelemetry.Context?.Cloud?.RoleName ?? "";
        ResponseCode = requestTelemetry.ResponseCode ?? "";
        Url = requestTelemetry.Url?.ToString() ?? "";
        
        // Custom = requestTelemetry.Metrics.ContainsKey("Custom")
        //     ? requestTelemetry.Metrics["Custom"]
        //     : (double?) null;
    }
}