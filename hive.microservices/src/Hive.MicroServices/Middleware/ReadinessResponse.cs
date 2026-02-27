using System.Text.Json.Serialization;
using Hive.HealthChecks;

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
  /// <param name="healthCheckStateProvider"></param>
  public ReadinessResponse(IMicroService service, IHealthCheckStateProvider? healthCheckStateProvider = null) : base(service)
  {
    Ready = service.IsReady;

    if (healthCheckStateProvider is not null)
    {
      Checks = healthCheckStateProvider.GetSnapshots()
        .Select(s => new HealthCheckEntry(s))
        .ToList();
    }
  }

  /// <summary>
  /// The readiness of the microservice
  /// </summary>
  [JsonPropertyName("ready")]
  public bool Ready { get; set; }

  /// <summary>
  /// Health check details. Null when no health checks are registered (backward compatible).
  /// </summary>
  [JsonPropertyName("checks")]
  public List<HealthCheckEntry>? Checks { get; set; }
}

/// <summary>
/// A single health check entry in the readiness response.
/// </summary>
public sealed class HealthCheckEntry
{
  internal HealthCheckEntry(HealthCheckStateSnapshot snapshot)
  {
    Name = snapshot.Name;
    Status = snapshot.Status;
    LastCheckedAt = snapshot.LastCheckedAt;
    DurationMs = snapshot.Duration?.TotalMilliseconds;
    AffectsReadiness = snapshot.AffectsReadiness;
    ReadinessThreshold = snapshot.ReadinessThreshold;
    ConsecutiveFailures = snapshot.ConsecutiveFailures;
    ConsecutiveSuccesses = snapshot.ConsecutiveSuccesses;
    IsPassingForReadiness = snapshot.IsPassingForReadiness;
  }

  /// <summary>The health check name.</summary>
  [JsonPropertyName("name")]
  public string Name { get; set; }

  /// <summary>The current status.</summary>
  [JsonPropertyName("status")]
  public HealthCheckStatus Status { get; set; }

  /// <summary>When the check was last evaluated.</summary>
  [JsonPropertyName("lastCheckedAt")]
  public DateTimeOffset? LastCheckedAt { get; set; }

  /// <summary>Duration of the last evaluation in milliseconds.</summary>
  [JsonPropertyName("durationMs")]
  public double? DurationMs { get; set; }

  /// <summary>Whether this check affects readiness.</summary>
  [JsonPropertyName("affectsReadiness")]
  public bool AffectsReadiness { get; set; }

  /// <summary>The readiness threshold for this check.</summary>
  [JsonPropertyName("readinessThreshold")]
  public ReadinessThreshold ReadinessThreshold { get; set; }

  /// <summary>Number of consecutive failures.</summary>
  [JsonPropertyName("consecutiveFailures")]
  public int ConsecutiveFailures { get; set; }

  /// <summary>Number of consecutive successes.</summary>
  [JsonPropertyName("consecutiveSuccesses")]
  public int ConsecutiveSuccesses { get; set; }

  /// <summary>Whether this check is currently passing for readiness.</summary>
  [JsonPropertyName("isPassingForReadiness")]
  public bool IsPassingForReadiness { get; set; }
}