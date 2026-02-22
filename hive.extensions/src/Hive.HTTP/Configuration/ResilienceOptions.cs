namespace Hive.HTTP.Configuration;

public class ResilienceOptions
{
  public int? MaxRetries { get; set; }

  public TimeSpan? PerAttemptTimeout { get; set; }

  public CircuitBreakerOptions? CircuitBreaker { get; set; }
}