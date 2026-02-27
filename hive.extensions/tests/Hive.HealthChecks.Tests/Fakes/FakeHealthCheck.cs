using Hive.HealthChecks;

namespace Hive.HealthChecks.Tests.Fakes;

public sealed class FakeHealthCheck : HiveHealthCheck, IHiveHealthCheck
{
  private readonly Func<CancellationToken, Task<HealthCheckStatus>> _evaluate;

  public static string CheckName => "Fake";

  public static void ConfigureDefaults(HiveHealthCheckOptions options)
  {
    options.AffectsReadiness = true;
    options.BlockReadinessProbeOnStartup = true;
  }

  public FakeHealthCheck(Func<CancellationToken, Task<HealthCheckStatus>> evaluate)
  {
    _evaluate = evaluate;
  }

  public override Task<HealthCheckStatus> EvaluateAsync(CancellationToken ct)
    => _evaluate(ct);

  public static FakeHealthCheck Healthy()
    => new(_ => Task.FromResult(HealthCheckStatus.Healthy));

  public static FakeHealthCheck Unhealthy()
    => new(_ => Task.FromResult(HealthCheckStatus.Unhealthy));

  public static FakeHealthCheck Degraded()
    => new(_ => Task.FromResult(HealthCheckStatus.Degraded));

  public static FakeHealthCheck Throwing(Exception ex)
    => new(_ => throw ex);

  public static FakeHealthCheck Delayed(TimeSpan delay, HealthCheckStatus status)
    => new(async ct =>
    {
      await Task.Delay(delay, ct);
      return status;
    });
}
