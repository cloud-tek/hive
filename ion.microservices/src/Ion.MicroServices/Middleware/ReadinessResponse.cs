using System.Text.Json.Serialization;

namespace Ion.Middleware;

public class ReadinessResponse : StartupResponse
{
    public ReadinessResponse(IMicroService service) : base(service)
    {
        Ready = service.IsReady;
    }

    public ReadinessResponse()
    {
    }

    [JsonPropertyName("ready")]
    public bool Ready { get; set; }
}