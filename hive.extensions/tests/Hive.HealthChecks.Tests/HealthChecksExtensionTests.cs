using FluentAssertions;
using Hive.HealthChecks;
using Hive.HealthChecks.Tests.Fakes;
using Hive.MicroServices;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hive.HealthChecks.Tests;

public class HealthChecksExtensionTests
{
  public class IntervalPriority
  {
    [Fact]
    [UnitTest]
    public void GivenNoOverrides_WhenConfigured_ThenDefaultIntervalUsed()
    {
      var builder = new HealthChecksBuilder();
      var config = new ConfigurationBuilder().Build();
      var extension = new HealthChecksExtension(
        new MicroService("test"), builder);

      var services = new ServiceCollection();
      extension.ConfigureServices(services, new MicroService("test"));

      // Trigger the ConfigureAction by building
      extension.ConfigureActions.Should().ContainSingle();
      var globalOptions = InvokeConfigureActionAndGetGlobalOptions(extension, config);

      globalOptions.Interval.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    [UnitTest]
    public void GivenIConfigurationInterval_WhenConfigured_ThenConfigIntervalUsed()
    {
      var builder = new HealthChecksBuilder();
      var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Hive:HealthChecks:Interval"] = "15"
        })
        .Build();

      var extension = new HealthChecksExtension(
        new MicroService("test"), builder);
      extension.ConfigureServices(new ServiceCollection(), new MicroService("test"));

      var globalOptions = InvokeConfigureActionAndGetGlobalOptions(extension, config);

      globalOptions.Interval.Should().Be(TimeSpan.FromSeconds(15));
    }

    [Fact]
    [UnitTest]
    public void GivenBuilderInterval_WhenConfigured_ThenBuilderIntervalOverridesConfig()
    {
      var builder = new HealthChecksBuilder { Interval = TimeSpan.FromSeconds(10) };
      var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Hive:HealthChecks:Interval"] = "15"
        })
        .Build();

      var extension = new HealthChecksExtension(
        new MicroService("test"), builder);
      extension.ConfigureServices(new ServiceCollection(), new MicroService("test"));

      var globalOptions = InvokeConfigureActionAndGetGlobalOptions(extension, config);

      globalOptions.Interval.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    [UnitTest]
    public void GivenOnlyBuilderInterval_WhenConfigured_ThenBuilderIntervalOverridesDefault()
    {
      var builder = new HealthChecksBuilder { Interval = TimeSpan.FromSeconds(45) };
      var config = new ConfigurationBuilder().Build();

      var extension = new HealthChecksExtension(
        new MicroService("test"), builder);
      extension.ConfigureServices(new ServiceCollection(), new MicroService("test"));

      var globalOptions = InvokeConfigureActionAndGetGlobalOptions(extension, config);

      globalOptions.Interval.Should().Be(TimeSpan.FromSeconds(45));
    }

    private static HealthChecksOptions InvokeConfigureActionAndGetGlobalOptions(
      HealthChecksExtension extension, IConfiguration config)
    {
      var services = new ServiceCollection();
      foreach (var action in extension.ConfigureActions)
      {
        action(services, config);
      }

      var sp = services.BuildServiceProvider();
      return sp.GetRequiredService<HealthCheckConfiguration>().GlobalOptions;
    }
  }

  public class ServiceRegistration
  {
    [Fact]
    [UnitTest]
    public void GivenExplicitChecks_WhenConfigured_ThenChecksRegisteredAsSingletons()
    {
      var builder = new HealthChecksBuilder();
      builder.WithHealthCheck<FakeHealthCheck>();

      var extension = new HealthChecksExtension(
        new MicroService("test"), builder);
      var services = new ServiceCollection();
      extension.ConfigureServices(services, new MicroService("test"));

      var config = new ConfigurationBuilder().Build();
      foreach (var action in extension.ConfigureActions)
      {
        action(services, config);
      }

      var descriptors = services.Where(d => d.ServiceType == typeof(HiveHealthCheck)).ToList();
      descriptors.Should().ContainSingle();
      descriptors[0].ImplementationType.Should().Be<FakeHealthCheck>();
      descriptors[0].Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    [UnitTest]
    public void GivenExtension_WhenConfigured_ThenRegistryIsRegistered()
    {
      var builder = new HealthChecksBuilder();
      var extension = new HealthChecksExtension(
        new MicroService("test"), builder);
      var services = new ServiceCollection();
      extension.ConfigureServices(services, new MicroService("test"));

      var config = new ConfigurationBuilder().Build();
      foreach (var action in extension.ConfigureActions)
      {
        action(services, config);
      }

      services.Should().Contain(d =>
        d.ServiceType == typeof(HealthCheckRegistry) &&
        d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    [UnitTest]
    public void GivenExtension_WhenConfigured_ThenActivitySourceNamesContainsHealthChecks()
    {
      var builder = new HealthChecksBuilder();
      var extension = new HealthChecksExtension(
        new MicroService("test"), builder);

      extension.ActivitySourceNames.Should().Contain(HealthCheckActivitySource.Name);
    }
  }
}
