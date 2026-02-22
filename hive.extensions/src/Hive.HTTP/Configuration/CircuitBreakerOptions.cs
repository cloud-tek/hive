namespace Hive.HTTP.Configuration;

public class CircuitBreakerOptions
{
  public bool Enabled { get; set; }

  public double FailureRatio { get; set; } = 0.5;

  public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

  public int MinimumThroughput { get; set; } = 10;

  public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);
}