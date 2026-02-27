using FluentAssertions;
using Hive.HealthChecks;
using Hive.HealthChecks.Tests.Fakes;
using Hive.Testing;
using Xunit;

namespace Hive.HealthChecks.Tests;

public class ReflectionBridgeTests
{
  [Fact]
  [UnitTest]
  public void GivenFakeHealthCheck_WhenGetCheckName_ThenReturnsStaticCheckName()
  {
    var name = ReflectionBridge.GetCheckName(typeof(FakeHealthCheck));

    name.Should().Be("Fake");
  }

  [Fact]
  [UnitTest]
  public void GivenFakeHealthCheck_WhenInvokeConfigureDefaults_ThenOptionsAreConfigured()
  {
    var options = new HiveHealthCheckOptions
    {
      AffectsReadiness = false,
      BlockReadinessProbeOnStartup = false
    };

    ReflectionBridge.InvokeConfigureDefaults(typeof(FakeHealthCheck), options);

    // FakeHealthCheck.ConfigureDefaults sets both to true
    options.AffectsReadiness.Should().BeTrue();
    options.BlockReadinessProbeOnStartup.Should().BeTrue();
  }

  [Fact]
  [UnitTest]
  public void GivenAlternativeCheck_WhenGetCheckName_ThenReturnsDifferentName()
  {
    var name = ReflectionBridge.GetCheckName(typeof(AlternativeFakeHealthCheck));

    name.Should().Be("Alternative");
  }
}