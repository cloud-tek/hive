using Hive.HealthChecks;

namespace Hive.HealthChecks.Tests.Fakes;

public sealed class AlternativeFakeHealthCheck : HiveHealthCheck, IHiveHealthCheck
{
  private readonly Func<CancellationToken, Task<HealthCheckStatus>> _evaluate;

  public static string CheckName => "Alternative";

  public static void ConfigureDefaults(HiveHealthCheckOptions options)
  {
    options.AffectsReadiness = false;
    options.BlockReadinessProbeOnStartup = false;
  }

  public AlternativeFakeHealthCheck(Func<CancellationToken, Task<HealthCheckStatus>> evaluate)
  {
    _evaluate = evaluate;
  }

  public override Task<HealthCheckStatus> EvaluateAsync(CancellationToken ct)
    => _evaluate(ct);

  public static AlternativeFakeHealthCheck Healthy()
    => new(_ => Task.FromResult(HealthCheckStatus.Healthy));
}