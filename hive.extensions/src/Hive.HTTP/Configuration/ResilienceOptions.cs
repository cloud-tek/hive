namespace Hive.HTTP.Configuration;

/// <summary>
/// Configuration options for HTTP client resilience policies.
/// </summary>
public class ResilienceOptions
{
  /// <summary>
  /// Maximum number of retry attempts. Null disables retries.
  /// </summary>
  public int? MaxRetries { get; set; }

  /// <summary>
  /// Timeout applied to each individual attempt. Null disables per-attempt timeout.
  /// </summary>
  public TimeSpan? PerAttemptTimeout { get; set; }

  /// <summary>
  /// Circuit breaker configuration. Null disables the circuit breaker.
  /// </summary>
  public CircuitBreakerOptions? CircuitBreaker { get; set; }
}