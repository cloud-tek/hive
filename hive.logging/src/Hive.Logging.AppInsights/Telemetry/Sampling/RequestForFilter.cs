using Microsoft.ApplicationInsights.DataContracts;

namespace Hive.Logging.AppInsights.Telemetry.Sampling;

/// <summary>
/// A struct that represents a request telemetry for filtering.
/// </summary>
[Serializable]
internal struct RequestForFilter
{
  /// <summary>
  /// Gets the cloud role name.
  /// </summary>
  public string CloudRoleName { get; }

  /// <summary>
  /// Gets the name of the request.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// Gets the duration of the request.
  /// </summary>
  public double Duration { get; }

  /// <summary>
  /// Gets a value indicating whether the request was successful.
  /// </summary>
  public bool? Success { get; }

  /// <summary>
  /// Gets the response code of the request.
  /// </summary>
  public string ResponseCode { get; set; }

  /// <summary>
  /// Gets the URL of the request.
  /// </summary>
  public string Url { get; }

  /// <summary>
  /// Initializes a new instance of the <see cref="RequestForFilter"/> struct.
  /// </summary>
  /// <param name="requestTelemetry"></param>
  public RequestForFilter(RequestTelemetry requestTelemetry)
  {
    Name = requestTelemetry.Name ?? "";
    Duration = requestTelemetry.Duration.TotalMilliseconds;
    Success = requestTelemetry.Success;
    CloudRoleName = requestTelemetry.Context?.Cloud?.RoleName ?? "";
    ResponseCode = requestTelemetry.ResponseCode ?? "";
    Url = requestTelemetry.Url?.ToString() ?? "";
  }
}