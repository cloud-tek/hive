namespace Hive.HTTP.Configuration;

/// <summary>
/// Configuration options for the HTTP client circuit breaker resilience policy.
/// </summary>
public class CircuitBreakerOptions
{
  /// <summary>
  /// Whether the circuit breaker is enabled.
  /// </summary>
  public bool Enabled { get; set; }

  /// <summary>
  /// The failure-to-success ratio that trips the circuit breaker.
  /// </summary>
  public double FailureRatio { get; set; } = 0.5;

  /// <summary>
  /// The time window over which failures are sampled.
  /// </summary>
  public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

  /// <summary>
  /// The minimum number of requests in the sampling window before the breaker can trip.
  /// </summary>
  public int MinimumThroughput { get; set; } = 10;

  /// <summary>
  /// How long the circuit stays open before transitioning to half-open.
  /// </summary>
  public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);
}