using System.Text.Json.Serialization;

namespace Hive.Middleware;

public class MiddlewareResponse
{
    public MiddlewareResponse(IMicroService service)
    {
        Name = service.Name;
        Id = service.Id;
        HostingMode = service.HostingMode;
        PipelineMode = service.PipelineMode;
        //Address = service.Address.ToString();
    }

    public MiddlewareResponse()
    {
    }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("hostingMode")]
    public MicroServiceHostingMode HostingMode { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("pipelineMode")]
    public MicroServicePipelineMode PipelineMode { get; set; }
}
