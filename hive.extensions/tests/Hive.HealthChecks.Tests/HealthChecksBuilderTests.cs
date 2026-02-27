using FluentAssertions;
using Hive.HealthChecks;
using Hive.HealthChecks.Tests.Fakes;
using Hive.Testing;
using Xunit;

namespace Hive.HealthChecks.Tests;

public class HealthChecksBuilderTests
{
  [Fact]
  [UnitTest]
  public void GivenBuilder_WhenWithHealthCheckCalled_ThenCheckIsRegistered()
  {
    var builder = new HealthChecksBuilder();
    builder.WithHealthCheck<FakeHealthCheck>();

    var registrations = builder.GetRegistrations();

    registrations.Should().ContainKey(typeof(FakeHealthCheck));
  }

  [Fact]
  [UnitTest]
  public void GivenBuilder_WhenWithHealthCheckCalledTwiceForSameType_ThenThrows()
  {
    var builder = new HealthChecksBuilder();
    builder.WithHealthCheck<FakeHealthCheck>();

    var act = () => builder.WithHealthCheck<FakeHealthCheck>();

    act.Should().Throw<InvalidOperationException>()
      .WithMessage("*FakeHealthCheck*already been registered*");
  }

  [Fact]
  [UnitTest]
  public void GivenBuilder_WhenConfigureCallbackProvided_ThenOptionsAreModified()
  {
    var builder = new HealthChecksBuilder();
    builder.WithHealthCheck<FakeHealthCheck>(o => o.AffectsReadiness = false);

    var options = builder.GetRegistrations()[typeof(FakeHealthCheck)];

    options.AffectsReadiness.Should().BeFalse();
  }

  [Fact]
  [UnitTest]
  public void GivenBuilder_WhenNoCallbackProvided_ThenConfigureDefaultsApplied()
  {
    var builder = new HealthChecksBuilder();
    builder.WithHealthCheck<FakeHealthCheck>();

    var options = builder.GetRegistrations()[typeof(FakeHealthCheck)];

    // FakeHealthCheck.ConfigureDefaults sets AffectsReadiness=true, BlockReadinessProbeOnStartup=true
    options.AffectsReadiness.Should().BeTrue();
    options.BlockReadinessProbeOnStartup.Should().BeTrue();
  }

  [Fact]
  [UnitTest]
  public void GivenBuilder_WhenMultipleCheckTypes_ThenAllRegistered()
  {
    var builder = new HealthChecksBuilder();
    builder
      .WithHealthCheck<FakeHealthCheck>()
      .WithHealthCheck<AlternativeFakeHealthCheck>();

    builder.GetRegistrations().Should().HaveCount(2);
  }

  [Fact]
  [UnitTest]
  public void GivenBuilder_WhenIntervalSet_ThenIntervalHasValue()
  {
    var builder = new HealthChecksBuilder();
    builder.Interval = TimeSpan.FromSeconds(15);

    builder.Interval.Should().Be(TimeSpan.FromSeconds(15));
  }

  [Fact]
  [UnitTest]
  public void GivenBuilder_WhenIntervalNotSet_ThenIntervalIsNull()
  {
    var builder = new HealthChecksBuilder();

    builder.Interval.Should().BeNull();
  }
}