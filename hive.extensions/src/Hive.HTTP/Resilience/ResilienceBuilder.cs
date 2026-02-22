using Hive.HTTP.Configuration;

namespace Hive.HTTP.Resilience;

public sealed class ResilienceBuilder
{
  internal ResilienceOptions Options { get; } = new();

  public ResilienceBuilder WithRetry(int maxRetries)
  {
    Options.MaxRetries = maxRetries;
    return this;
  }

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

  public ResilienceBuilder WithTimeout(TimeSpan perAttemptTimeout)
  {
    Options.PerAttemptTimeout = perAttemptTimeout;
    return this;
  }

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