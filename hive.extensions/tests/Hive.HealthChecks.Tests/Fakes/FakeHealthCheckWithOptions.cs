using Hive.HealthChecks;

namespace Hive.HealthChecks.Tests.Fakes;

public sealed class FakeCheckOptions
{
  public string Endpoint { get; set; } = string.Empty;
  public int RetryCount { get; set; }
}

public sealed class FakeHealthCheckWithOptions : HiveHealthCheck<FakeCheckOptions>, IHiveHealthCheck
{
  public static string CheckName => "FakeWithOptions";

  public static void ConfigureDefaults(HiveHealthCheckOptions options)
  {
    options.AffectsReadiness = true;
    options.BlockReadinessProbeOnStartup = false;
  }

  public override Task<HealthCheckStatus> EvaluateAsync(CancellationToken ct)
    => Task.FromResult(HealthCheckStatus.Healthy);
}
