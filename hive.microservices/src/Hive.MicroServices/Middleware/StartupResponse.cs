using System.Text.Json.Serialization;

namespace Hive.Middleware;

/// <summary>
/// The startup response.
/// </summary>
public class StartupResponse : MiddlewareResponse
{
  /// <summary>
  /// Creates a new <see cref="StartupResponse"/> instance
  /// </summary>
  /// <param name="service"></param>
  public StartupResponse(IMicroService service)
  : base(service)
  {
    Started = service.IsStarted;
  }

  /// <summary>
  /// The startup status of the microservice
  /// </summary>
  [JsonPropertyName("started")]
  public bool Started { get; set; }
}