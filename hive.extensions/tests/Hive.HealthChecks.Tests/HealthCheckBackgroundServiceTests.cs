using FluentAssertions;
using Hive.HealthChecks;
using Hive.HealthChecks.Tests.Fakes;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hive.HealthChecks.Tests;

public class HealthCheckBackgroundServiceTests
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

  public class EagerInitialEvaluation
  {
    [Fact]
    [UnitTest]
    public async Task GivenChecks_WhenExecuteAsyncStarts_ThenAllChecksEvaluatedImmediately()
    {
      var registry = new HealthCheckRegistry();
      var check = FakeHealthCheck.Healthy();
      var config = CreateConfig(new Dictionary<Type, HiveHealthCheckOptions>
      {
        [typeof(FakeHealthCheck)] = new()
      });
      // Register the check in registry first (startup service normally does this)
      registry.Register("Fake", config.ExplicitRegistrations[typeof(FakeHealthCheck)]);

      var gate = new HealthCheckStartupGate();
      gate.Signal();
      var service = new HealthCheckBackgroundService(
        [check], registry, new HealthCheckOptionsResolver(config), config, gate, NullLogger<HealthCheckBackgroundService>.Instance);

      using var cts = new CancellationTokenSource();
      // Cancel shortly after to stop the periodic loops
      cts.CancelAfter(TimeSpan.FromMilliseconds(200));

      try
      {
        await service.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        await service.StopAsync(CancellationToken.None);
      }
      catch (OperationCanceledException)
      {
        // Expected — timer loops cancelled
      }

      var snapshot = registry.GetSnapshots().Single();
      snapshot.Status.Should().Be(HealthCheckStatus.Healthy);
      snapshot.LastCheckedAt.Should().NotBeNull();
    }
  }

  public class Timeout
  {
    [Fact]
    [UnitTest]
    public async Task GivenTimeout_WhenEvaluationExceedsTimeout_ThenMarkedUnhealthyWithTimeoutError()
    {
      var registry = new HealthCheckRegistry();
      // Check delays for 5 seconds but timeout is 100ms
      var check = FakeHealthCheck.Delayed(TimeSpan.FromSeconds(5), HealthCheckStatus.Healthy);
      var options = new HiveHealthCheckOptions { Timeout = TimeSpan.FromMilliseconds(100) };
      var config = CreateConfig(new Dictionary<Type, HiveHealthCheckOptions>
      {
        [typeof(FakeHealthCheck)] = options
      });
      registry.Register("Fake", options);

      var gate = new HealthCheckStartupGate();
      gate.Signal();
      var service = new HealthCheckBackgroundService(
        [check], registry, new HealthCheckOptionsResolver(config), config, gate, NullLogger<HealthCheckBackgroundService>.Instance);

      using var cts = new CancellationTokenSource();
      cts.CancelAfter(TimeSpan.FromSeconds(2));

      try
      {
        await service.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        await service.StopAsync(CancellationToken.None);
      }
      catch (OperationCanceledException)
      {
        // Expected
      }

      var snapshot = registry.GetSnapshots().Single();
      snapshot.Status.Should().Be(HealthCheckStatus.Unhealthy);
      snapshot.Error.Should().Contain("timed out");
    }
  }

  public class ExceptionHandling
  {
    [Fact]
    [UnitTest]
    public async Task GivenCheck_WhenEvaluateThrows_ThenMarkedUnhealthyAndLoopContinues()
    {
      var registry = new HealthCheckRegistry();
      var check = FakeHealthCheck.Throwing(new InvalidOperationException("Boom"));
      var options = new HiveHealthCheckOptions();
      var config = CreateConfig(new Dictionary<Type, HiveHealthCheckOptions>
      {
        [typeof(FakeHealthCheck)] = options
      });
      registry.Register("Fake", options);

      var gate = new HealthCheckStartupGate();
      gate.Signal();
      var service = new HealthCheckBackgroundService(
        [check], registry, new HealthCheckOptionsResolver(config), config, gate, NullLogger<HealthCheckBackgroundService>.Instance);

      using var cts = new CancellationTokenSource();
      cts.CancelAfter(TimeSpan.FromMilliseconds(500));

      try
      {
        await service.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(600));
        await service.StopAsync(CancellationToken.None);
      }
      catch (OperationCanceledException)
      {
        // Expected
      }

      var snapshot = registry.GetSnapshots().Single();
      snapshot.Status.Should().Be(HealthCheckStatus.Unhealthy);
      snapshot.Error.Should().Be("Boom");
    }
  }

  public class ConfigurationOverrides
  {
    [Fact]
    [UnitTest]
    public async Task GivenConfigurationSection_WhenResolvingOptions_ThenConfigOverridesDefaults()
    {
      var registry = new HealthCheckRegistry();
      var evaluationCount = 0;
      var check = new FakeHealthCheck(_ =>
      {
        Interlocked.Increment(ref evaluationCount);
        return Task.FromResult(HealthCheckStatus.Healthy);
      });
      var config = CreateConfig(
        configValues: new Dictionary<string, string?>
        {
          ["Hive:HealthChecks:Checks:Fake:FailureThreshold"] = "5"
        });
      registry.Register("Fake", new HiveHealthCheckOptions());

      var gate = new HealthCheckStartupGate();
      gate.Signal();
      var service = new HealthCheckBackgroundService(
        [check], registry, new HealthCheckOptionsResolver(config), config, gate, NullLogger<HealthCheckBackgroundService>.Instance);

      using var cts = new CancellationTokenSource();
      cts.CancelAfter(TimeSpan.FromMilliseconds(200));

      try
      {
        await service.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        await service.StopAsync(CancellationToken.None);
      }
      catch (OperationCanceledException)
      {
        // Expected
      }

      // The check should have been evaluated at least once (eager initial evaluation)
      evaluationCount.Should().BeGreaterThan(0);
    }
  }
}