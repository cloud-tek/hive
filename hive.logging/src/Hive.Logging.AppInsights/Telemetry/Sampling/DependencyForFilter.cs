using Microsoft.ApplicationInsights.DataContracts;

namespace Hive.Logging.AppInsights.Telemetry.Sampling;

/// <summary>
/// A struct that represents a dependency telemetry for filtering.
/// </summary>
[Serializable]
internal struct DependencyForFilter
{
  /// <summary>
  /// Gets or sets the cloud role name.
  /// </summary>
  public string CloudRoleName { get; set; }

  /// <summary>
  /// Gets or sets the type of the dependency.
  /// </summary>
  public string Type { get; set; }

  /// <summary>
  /// Gets or sets the target of the dependency.
  /// </summary>
  public string Target { get; }

  /// <summary>
  /// Gets or sets the name of the dependency.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// Gets or sets the duration of the dependency.
  /// </summary>
  public double Duration { get; }

  /// <summary>
  /// Gets or sets a value indicating whether the dependency was successful.
  /// </summary>
  public bool? Success { get; }

  /// <summary>
  /// Gets or sets the result code of the dependency.
  /// </summary>
  public string ResultCode { get; }

  /// <summary>
  /// Initializes a new instance of the <see cref="DependencyForFilter"/> struct.
  /// </summary>
  /// <param name="dependencyTelemetry"></param>
  public DependencyForFilter(DependencyTelemetry dependencyTelemetry)
  {
    Type = dependencyTelemetry.Type ?? "";
    Target = dependencyTelemetry.Target ?? "";
    Name = dependencyTelemetry.Name ?? "";
    Duration = dependencyTelemetry.Duration.TotalMilliseconds;
    Success = dependencyTelemetry.Success;
    CloudRoleName = dependencyTelemetry.Context?.Cloud?.RoleName ?? "";
    ResultCode = dependencyTelemetry.ResultCode ?? "";
  }
}