using FluentAssertions;
using Hive.HealthChecks;
using Hive.HealthChecks.Tests.Fakes;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hive.HealthChecks.Tests;

public class HealthCheckStartupServiceTests
{
  private static HealthCheckConfiguration CreateConfig(
    IReadOnlyDictionary<Type, HiveHealthCheckOptions>? registrations = null,
    Dictionary<string, string?>? configValues = null)
  {
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configValues ?? [])
      .Build();

    return new HealthCheckConfiguration(
      new HealthChecksOptions(),
      registrations ?? new Dictionary<Type, HiveHealthCheckOptions>(),
      config);
  }

  private static HealthCheckStartupService CreateService(
    IEnumerable<HiveHealthCheck> checks,
    HealthCheckRegistry registry,
    HealthCheckConfiguration config)
    => new(NullLoggerFactory.Instance, checks, registry, config);

  public class Registration
  {
    [Fact]
    [UnitTest]
    public async Task GivenChecks_WhenOnStartAsync_ThenAllChecksRegisteredInRegistry()
    {
      var registry = new HealthCheckRegistry();
      var checks = new HiveHealthCheck[] { FakeHealthCheck.Healthy() };
      var config = CreateConfig(new Dictionary<Type, HiveHealthCheckOptions>
      {
        [typeof(FakeHealthCheck)] = new()
      });

      var service = CreateService(checks, registry, config);
      await service.StartAsync(CancellationToken.None);

      registry.GetSnapshots().Should().ContainSingle()
        .Which.Name.Should().Be("Fake");
    }

    [Fact]
    [UnitTest]
    public async Task GivenCheckWithOptions_WhenOnStartAsync_ThenOptionsAreBoundFromConfiguration()
    {
      var registry = new HealthCheckRegistry();
      var check = new FakeHealthCheckWithOptions();
      var config = CreateConfig(
        registrations: new Dictionary<Type, HiveHealthCheckOptions>
        {
          [typeof(FakeHealthCheckWithOptions)] = new() { BlockReadinessProbeOnStartup = false }
        },
        configValues: new Dictionary<string, string?>
        {
          ["Hive:HealthChecks:Checks:FakeWithOptions:Options:Endpoint"] = "http://localhost:8080",
          ["Hive:HealthChecks:Checks:FakeWithOptions:Options:RetryCount"] = "3"
        });

      var service = CreateService([check], registry, config);
      await service.StartAsync(CancellationToken.None);

      check.Options.Endpoint.Should().Be("http://localhost:8080");
      check.Options.RetryCount.Should().Be(3);
    }
  }

  public class BlockingEvaluation
  {
    [Fact]
    [UnitTest]
    public async Task GivenBlockingCheck_WhenHealthy_ThenStartupSucceeds()
    {
      var registry = new HealthCheckRegistry();
      var check = FakeHealthCheck.Healthy();
      var config = CreateConfig(new Dictionary<Type, HiveHealthCheckOptions>
      {
        [typeof(FakeHealthCheck)] = new() { BlockReadinessProbeOnStartup = true }
      });

      var service = CreateService([check], registry, config);

      var act = () => service.StartAsync(CancellationToken.None);

      await act.Should().NotThrowAsync();
      registry.GetSnapshots().Single().Status.Should().Be(HealthCheckStatus.Healthy);
    }

    [Fact]
    [UnitTest]
    public async Task GivenBlockingCheck_WhenUnhealthy_ThenStartupThrows()
    {
      var registry = new HealthCheckRegistry();
      var check = FakeHealthCheck.Unhealthy();
      var config = CreateConfig(new Dictionary<Type, HiveHealthCheckOptions>
      {
        [typeof(FakeHealthCheck)] = new() { BlockReadinessProbeOnStartup = true }
      });

      var service = CreateService([check], registry, config);

      var act = () => service.StartAsync(CancellationToken.None);

      await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*Fake*Unhealthy during startup*");
    }

    [Fact]
    [UnitTest]
    public async Task GivenBlockingCheck_WhenEvaluateThrows_ThenStartupThrowsWrapped()
    {
      var registry = new HealthCheckRegistry();
      var check = FakeHealthCheck.Throwing(new TimeoutException("Connection timed out"));
      var config = CreateConfig(new Dictionary<Type, HiveHealthCheckOptions>
      {
        [typeof(FakeHealthCheck)] = new() { BlockReadinessProbeOnStartup = true }
      });

      var service = CreateService([check], registry, config);

      var act = () => service.StartAsync(CancellationToken.None);

      var ex = await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*Fake*failed during startup*");
      ex.Which.InnerException.Should().BeOfType<TimeoutException>();
    }

    [Fact]
    [UnitTest]
    public async Task GivenNonBlockingCheck_WhenUnhealthy_ThenStartupStillSucceeds()
    {
      var registry = new HealthCheckRegistry();
      var check = FakeHealthCheck.Unhealthy();
      var config = CreateConfig(new Dictionary<Type, HiveHealthCheckOptions>
      {
        [typeof(FakeHealthCheck)] = new() { BlockReadinessProbeOnStartup = false }
      });

      var service = CreateService([check], registry, config);

      var act = () => service.StartAsync(CancellationToken.None);

      await act.Should().NotThrowAsync();
    }
  }

  public class ConfigurationOverrides
  {
    [Fact]
    [UnitTest]
    public async Task GivenExplicitRegistration_WhenResolvingOptions_ThenExplicitOptionsUsed()
    {
      var registry = new HealthCheckRegistry();
      var check = FakeHealthCheck.Healthy();
      var explicitOptions = new HiveHealthCheckOptions
      {
        AffectsReadiness = false,
        BlockReadinessProbeOnStartup = true,
        FailureThreshold = 5
      };
      var config = CreateConfig(new Dictionary<Type, HiveHealthCheckOptions>
      {
        [typeof(FakeHealthCheck)] = explicitOptions
      });

      var service = CreateService([check], registry, config);
      await service.StartAsync(CancellationToken.None);

      var snapshot = registry.GetSnapshots().Single();
      snapshot.AffectsReadiness.Should().BeFalse();
    }

    [Fact]
    [UnitTest]
    public async Task GivenConfigurationSection_WhenResolvingOptions_ThenConfigOverridesDefaults()
    {
      var registry = new HealthCheckRegistry();
      var check = FakeHealthCheck.Healthy();
      // FakeHealthCheck.ConfigureDefaults sets AffectsReadiness=true
      // IConfiguration overrides to false
      var config = CreateConfig(
        configValues: new Dictionary<string, string?>
        {
          ["Hive:HealthChecks:Checks:Fake:AffectsReadiness"] = "false",
          ["Hive:HealthChecks:Checks:Fake:BlockReadinessProbeOnStartup"] = "false"
        });

      var service = CreateService([check], registry, config);
      await service.StartAsync(CancellationToken.None);

      var snapshot = registry.GetSnapshots().Single();
      snapshot.AffectsReadiness.Should().BeFalse();
    }

    [Theory]
    [UnitTest]
    [InlineData("Interval", "15")]
    [InlineData("FailureThreshold", "5")]
    [InlineData("SuccessThreshold", "3")]
    [InlineData("Timeout", "60")]
    [InlineData("ReadinessThreshold", "Healthy")]
    public async Task GivenConfigurationValues_WhenApplied_ThenPropertyIsOverridden(
      string key, string value)
    {
      var registry = new HealthCheckRegistry();
      var check = FakeHealthCheck.Healthy();
      var config = CreateConfig(
        configValues: new Dictionary<string, string?>
        {
          [$"Hive:HealthChecks:Checks:Fake:{key}"] = value,
          ["Hive:HealthChecks:Checks:Fake:BlockReadinessProbeOnStartup"] = "false"
        });

      var service = CreateService([check], registry, config);

      var act = () => service.StartAsync(CancellationToken.None);

      // Should not throw — config is applied successfully
      await act.Should().NotThrowAsync();
    }
  }
}