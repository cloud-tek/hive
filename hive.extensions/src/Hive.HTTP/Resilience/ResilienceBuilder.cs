using Hive.HTTP.Configuration;

namespace Hive.HTTP.Resilience;

/// <summary>
/// Fluent builder for configuring HTTP client resilience policies (retry, circuit breaker, timeout).
/// </summary>
public sealed class ResilienceBuilder
{
  internal ResilienceOptions Options { get; } = new();

  /// <summary>
  /// Configures retry with the specified maximum number of attempts.
  /// </summary>
  /// <param name="maxRetries">The maximum number of retry attempts.</param>
  /// <returns>The builder instance for chaining.</returns>
  public ResilienceBuilder WithRetry(int maxRetries)
  {
    Options.MaxRetries = maxRetries;
    return this;
  }

  /// <summary>
  /// Configures a circuit breaker with optional parameter overrides.
  /// </summary>
  /// <param name="failureRatio">The failure ratio that trips the breaker.</param>
  /// <param name="samplingDuration">The time window for failure sampling.</param>
  /// <param name="minimumThroughput">The minimum request count before the breaker can trip.</param>
  /// <param name="breakDuration">How long the circuit stays open.</param>
  /// <returns>The builder instance for chaining.</returns>
  public ResilienceBuilder WithCircuitBreaker(
    double? failureRatio = null,
    TimeSpan? samplingDuration = null,
    int? minimumThroughput = null,
    TimeSpan? breakDuration = null)
  {
    Options.CircuitBreaker ??= new CircuitBreakerOptions { Enabled = true };
    Options.CircuitBreaker.Enabled = true;

    if (failureRatio.HasValue)
      Options.CircuitBreaker.FailureRatio = failureRatio.Value;

    if (samplingDuration.HasValue)
      Options.CircuitBreaker.SamplingDuration = samplingDuration.Value;

    if (minimumThroughput.HasValue)
      Options.CircuitBreaker.MinimumThroughput = minimumThroughput.Value;

    if (breakDuration.HasValue)
      Options.CircuitBreaker.BreakDuration = breakDuration.Value;

    return this;
  }

  /// <summary>
  /// Configures a per-attempt timeout.
  /// </summary>
  /// <param name="perAttemptTimeout">The timeout duration for each individual attempt.</param>
  /// <returns>The builder instance for chaining.</returns>
  public ResilienceBuilder WithTimeout(TimeSpan perAttemptTimeout)
  {
    Options.PerAttemptTimeout = perAttemptTimeout;
    return this;
  }

  /// <summary>
  /// Applies a standard resilience preset: 3 retries, 10s timeout, and circuit breaker with 10% failure ratio.
  /// </summary>
  /// <returns>The builder instance for chaining.</returns>
  public ResilienceBuilder UseStandardResilience()
  {
    Options.MaxRetries = 3;
    Options.PerAttemptTimeout = TimeSpan.FromSeconds(10);
    Options.CircuitBreaker = new CircuitBreakerOptions
    {
      Enabled = true,
      FailureRatio = 0.1,
      SamplingDuration = TimeSpan.FromSeconds(30),
      MinimumThroughput = 10,
      BreakDuration = TimeSpan.FromSeconds(30)
    };
    return this;
  }
}