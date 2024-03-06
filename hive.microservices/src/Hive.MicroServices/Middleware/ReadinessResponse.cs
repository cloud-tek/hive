using System.Text.Json.Serialization;

namespace Hive.Middleware;

/// <summary>
/// The readiness response.
/// </summary>
public class ReadinessResponse : StartupResponse
{
  /// <summary>
  /// Creates a new <see cref="ReadinessResponse"/> instance
  /// </summary>
  /// <param name="service"></param>
  public ReadinessResponse(IMicroService service) : base(service)
  {
    Ready = service.IsReady;
  }

  /// <summary>
  /// The readiness of the microservice
  /// </summary>
  [JsonPropertyName("ready")]
  public bool Ready { get; set; }
}