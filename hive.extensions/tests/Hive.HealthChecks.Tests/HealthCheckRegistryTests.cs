using FluentAssertions;
using Hive.HealthChecks;
using CloudTek.Testing;
using Xunit;

namespace Hive.HealthChecks.Tests;

public class HealthCheckRegistryTests
{
  private static HiveHealthCheckOptions DefaultOptions(
    int failureThreshold = 1,
    int successThreshold = 1,
    ReadinessThreshold readinessThreshold = ReadinessThreshold.Degraded,
    bool affectsReadiness = true)
  {
    return new HiveHealthCheckOptions
    {
      AffectsReadiness = affectsReadiness,
      FailureThreshold = failureThreshold,
      SuccessThreshold = successThreshold,
      ReadinessThreshold = readinessThreshold
    };
  }

  public class Register
  {
    [Fact]
    [UnitTest]
    public void GivenNewCheck_WhenRegistered_ThenSnapshotHasUnknownStatus()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions());

      var snapshots = registry.GetSnapshots();

      snapshots.Should().ContainSingle()
        .Which.Status.Should().Be(HealthCheckStatus.Unknown);
    }

    [Fact]
    [UnitTest]
    public void GivenNewCheck_WhenRegistered_ThenIsPassingForReadinessIsTrue()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions());

      var snapshot = registry.GetSnapshots().Single();

      snapshot.IsPassingForReadiness.Should().BeTrue();
    }

    [Fact]
    [UnitTest]
    public void GivenMultipleChecks_WhenRegistered_ThenAllAppearInSnapshots()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("check-a", DefaultOptions());
      registry.Register("check-b", DefaultOptions());
      registry.Register("check-c", DefaultOptions());

      var snapshots = registry.GetSnapshots();

      snapshots.Should().HaveCount(3);
      snapshots.Select(s => s.Name).Should().BeEquivalentTo("check-a", "check-b", "check-c");
    }
  }

  public class UpdateAndRecompute
  {
    [Fact]
    [UnitTest]
    public void GivenRegisteredCheck_WhenHealthy_ThenStatusIsHealthy()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions());
      registry.UpdateAndRecompute("test", HealthCheckStatus.Healthy, TimeSpan.FromMilliseconds(50), null);

      registry.GetSnapshots().Single().Status.Should().Be(HealthCheckStatus.Healthy);
    }

    [Fact]
    [UnitTest]
    public void GivenRegisteredCheck_WhenUnhealthy_ThenStatusIsUnhealthy()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions());
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.FromMilliseconds(50), null);

      registry.GetSnapshots().Single().Status.Should().Be(HealthCheckStatus.Unhealthy);
    }

    [Fact]
    [UnitTest]
    public void GivenRegisteredCheck_WhenDegraded_ThenStatusIsDegraded()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions());
      registry.UpdateAndRecompute("test", HealthCheckStatus.Degraded, TimeSpan.FromMilliseconds(50), null);

      registry.GetSnapshots().Single().Status.Should().Be(HealthCheckStatus.Degraded);
    }

    [Fact]
    [UnitTest]
    public void GivenRegisteredCheck_WhenUpdated_ThenDurationIsRecorded()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions());
      var duration = TimeSpan.FromMilliseconds(123);

      registry.UpdateAndRecompute("test", HealthCheckStatus.Healthy, duration, null);

      registry.GetSnapshots().Single().Duration.Should().Be(duration);
    }

    [Fact]
    [UnitTest]
    public void GivenRegisteredCheck_WhenUpdated_ThenLastCheckedAtIsSet()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions());
      var before = DateTimeOffset.UtcNow;

      registry.UpdateAndRecompute("test", HealthCheckStatus.Healthy, TimeSpan.Zero, null);

      var snapshot = registry.GetSnapshots().Single();
      snapshot.LastCheckedAt.Should().NotBeNull();
      snapshot.LastCheckedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    [UnitTest]
    public void GivenRegisteredCheck_WhenUnhealthyWithError_ThenErrorIsRecorded()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions());

      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, "Connection refused");

      registry.GetSnapshots().Single().Error.Should().Be("Connection refused");
    }

    [Fact]
    [UnitTest]
    public void GivenRegisteredCheck_WhenHealthyAfterError_ThenErrorIsCleared()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions());
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, "Connection refused");
      registry.UpdateAndRecompute("test", HealthCheckStatus.Healthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().Error.Should().BeNull();
    }

    [Fact]
    [UnitTest]
    public void GivenUnregisteredCheck_WhenUpdated_ThenNoEffect()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions());

      registry.UpdateAndRecompute("nonexistent", HealthCheckStatus.Healthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Should().ContainSingle()
        .Which.Name.Should().Be("test");
    }
  }

  public class ConsecutiveCounters
  {
    [Fact]
    [UnitTest]
    public void GivenHealthyCheck_WhenHealthyAgain_ThenConsecutiveSuccessesIncrements()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions());
      registry.UpdateAndRecompute("test", HealthCheckStatus.Healthy, TimeSpan.Zero, null);
      registry.UpdateAndRecompute("test", HealthCheckStatus.Healthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().ConsecutiveSuccesses.Should().Be(2);
    }

    [Fact]
    [UnitTest]
    public void GivenHealthyCheck_WhenUnhealthy_ThenConsecutiveSuccessesResets()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions());
      registry.UpdateAndRecompute("test", HealthCheckStatus.Healthy, TimeSpan.Zero, null);
      registry.UpdateAndRecompute("test", HealthCheckStatus.Healthy, TimeSpan.Zero, null);
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().ConsecutiveSuccesses.Should().Be(0);
    }

    [Fact]
    [UnitTest]
    public void GivenUnhealthyCheck_WhenUnhealthyAgain_ThenConsecutiveFailuresIncrements()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(failureThreshold: 5));
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().ConsecutiveFailures.Should().Be(2);
    }

    [Fact]
    [UnitTest]
    public void GivenUnhealthyCheck_WhenHealthy_ThenConsecutiveFailuresResets()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(failureThreshold: 5));
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);
      registry.UpdateAndRecompute("test", HealthCheckStatus.Healthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().ConsecutiveFailures.Should().Be(0);
    }
  }

  public class FailureThreshold
  {
    [Fact]
    [UnitTest]
    public void GivenFailureThresholdOf3_WhenFirstFailure_ThenStillPassingForReadiness()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(failureThreshold: 3));
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().IsPassingForReadiness.Should().BeTrue();
    }

    [Fact]
    [UnitTest]
    public void GivenFailureThresholdOf3_WhenSecondFailure_ThenStillPassingForReadiness()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(failureThreshold: 3));
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().IsPassingForReadiness.Should().BeTrue();
    }

    [Fact]
    [UnitTest]
    public void GivenFailureThresholdOf3_WhenThirdFailure_ThenNotPassingForReadiness()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(failureThreshold: 3));
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().IsPassingForReadiness.Should().BeFalse();
    }

    [Fact]
    [UnitTest]
    public void GivenFailureThresholdOf1_WhenFirstFailure_ThenImmediatelyNotPassing()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(failureThreshold: 1));
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().IsPassingForReadiness.Should().BeFalse();
    }
  }

  public class SuccessThreshold
  {
    [Fact]
    [UnitTest]
    public void GivenSuccessThresholdOf3_WhenFirstRecovery_ThenStillNotPassing()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(successThreshold: 3));
      // Drive into failed state first
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);
      // First recovery
      registry.UpdateAndRecompute("test", HealthCheckStatus.Healthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().IsPassingForReadiness.Should().BeFalse();
    }

    [Fact]
    [UnitTest]
    public void GivenSuccessThresholdOf3_WhenThirdRecovery_ThenPassingRestored()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(successThreshold: 3));
      // Drive into failed state
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);
      // Three recoveries
      registry.UpdateAndRecompute("test", HealthCheckStatus.Healthy, TimeSpan.Zero, null);
      registry.UpdateAndRecompute("test", HealthCheckStatus.Healthy, TimeSpan.Zero, null);
      registry.UpdateAndRecompute("test", HealthCheckStatus.Healthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().IsPassingForReadiness.Should().BeTrue();
    }

    [Fact]
    [UnitTest]
    public void GivenSuccessThresholdOf1_WhenFirstRecovery_ThenImmediatelyPassing()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(successThreshold: 1));
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);
      registry.UpdateAndRecompute("test", HealthCheckStatus.Healthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().IsPassingForReadiness.Should().BeTrue();
    }
  }

  public class ReadinessThresholdTests
  {
    [Fact]
    [UnitTest]
    public void GivenDegradedThreshold_WhenHealthy_ThenPassing()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(readinessThreshold: ReadinessThreshold.Degraded));
      registry.UpdateAndRecompute("test", HealthCheckStatus.Healthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().IsPassingForReadiness.Should().BeTrue();
    }

    [Fact]
    [UnitTest]
    public void GivenDegradedThreshold_WhenDegraded_ThenPassing()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(readinessThreshold: ReadinessThreshold.Degraded));
      registry.UpdateAndRecompute("test", HealthCheckStatus.Degraded, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().IsPassingForReadiness.Should().BeTrue();
    }

    [Fact]
    [UnitTest]
    public void GivenDegradedThreshold_WhenUnhealthy_ThenNotPassing()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(readinessThreshold: ReadinessThreshold.Degraded));
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().IsPassingForReadiness.Should().BeFalse();
    }

    [Fact]
    [UnitTest]
    public void GivenHealthyThreshold_WhenHealthy_ThenPassing()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(readinessThreshold: ReadinessThreshold.Healthy));
      registry.UpdateAndRecompute("test", HealthCheckStatus.Healthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().IsPassingForReadiness.Should().BeTrue();
    }

    [Fact]
    [UnitTest]
    public void GivenHealthyThreshold_WhenDegraded_ThenNotPassing()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(readinessThreshold: ReadinessThreshold.Healthy));
      registry.UpdateAndRecompute("test", HealthCheckStatus.Degraded, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().IsPassingForReadiness.Should().BeFalse();
    }

    [Fact]
    [UnitTest]
    public void GivenHealthyThreshold_WhenUnhealthy_ThenNotPassing()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(readinessThreshold: ReadinessThreshold.Healthy));
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().IsPassingForReadiness.Should().BeFalse();
    }

    [Fact]
    [UnitTest]
    public void GivenAnyThreshold_WhenUnknown_ThenNotPassing()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(readinessThreshold: ReadinessThreshold.Degraded));
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unknown, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().IsPassingForReadiness.Should().BeFalse();
    }
  }

  public class AffectsReadiness
  {
    [Fact]
    [UnitTest]
    public void GivenAffectsReadinessFalse_WhenUnhealthy_ThenSnapshotReportsAffectsReadinessFalse()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(affectsReadiness: false));
      registry.UpdateAndRecompute("test", HealthCheckStatus.Unhealthy, TimeSpan.Zero, null);

      registry.GetSnapshots().Single().AffectsReadiness.Should().BeFalse();
    }
  }

  public class ThreadSafety
  {
    [Fact]
    [UnitTest]
    public async Task GivenConcurrentUpdates_WhenManyThreadsUpdate_ThenNoCorruption()
    {
      var registry = new HealthCheckRegistry();
      registry.Register("test", DefaultOptions(failureThreshold: 100));

      var tasks = Enumerable.Range(0, 100).Select(i =>
        Task.Run(() =>
        {
          var status = i % 2 == 0 ? HealthCheckStatus.Healthy : HealthCheckStatus.Unhealthy;
          registry.UpdateAndRecompute("test", status, TimeSpan.FromMilliseconds(i), null);
        }));

      await Task.WhenAll(tasks);

      var snapshot = registry.GetSnapshots().Single();
      snapshot.Name.Should().Be("test");
      snapshot.LastCheckedAt.Should().NotBeNull();
    }
  }
}