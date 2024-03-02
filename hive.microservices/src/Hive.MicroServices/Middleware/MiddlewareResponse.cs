using System.Text.Json.Serialization;

namespace Hive.Middleware;

/// <summary>
/// The middleware response base-class.
/// </summary>
public class MiddlewareResponse
{
  /// <summary>
  /// Creates a new <see cref="MiddlewareResponse"/> instance
  /// </summary>
  /// <param name="service"></param>
  public MiddlewareResponse(IMicroService service)
  {
    Name = service.Name;
    Id = service.Id;
    HostingMode = service.HostingMode;
    PipelineMode = service.PipelineMode;
  }

  /// <summary>
  /// The hosting mode of the microservice
  /// </summary>
  [JsonPropertyName("hostingMode")]
  public MicroServiceHostingMode HostingMode { get; set; }

  /// <summary>
  /// The id of the microservice
  /// </summary>
  [JsonPropertyName("id")]
  public string Id { get; set; }

  /// <summary>
  /// The name of the microservice
  /// </summary>
  [JsonPropertyName("name")]
  public string Name { get; set; }

  /// <summary>
  /// The pipeline mode of the microservice
  /// </summary>
  [JsonPropertyName("pipelineMode")]
  public MicroServicePipelineMode PipelineMode { get; set; }
}