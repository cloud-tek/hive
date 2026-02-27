using FluentAssertions;
using Hive.HealthChecks;
using Hive.Testing;
using Xunit;

namespace Hive.HealthChecks.Tests;

public class HiveHealthCheckOptionsTests
{
  [Fact]
  [UnitTest]
  public void GivenNewOptions_WhenCreated_ThenDefaultsAreCorrect()
  {
    var options = new HiveHealthCheckOptions();

    options.Interval.Should().BeNull();
    options.AffectsReadiness.Should().BeTrue();
    options.BlockReadinessProbeOnStartup.Should().BeTrue();
    options.ReadinessThreshold.Should().Be(ReadinessThreshold.Degraded);
    options.FailureThreshold.Should().Be(1);
    options.SuccessThreshold.Should().Be(1);
    options.Timeout.Should().Be(TimeSpan.FromSeconds(30));
  }
}