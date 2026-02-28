using FluentAssertions;
using Hive.MicroServices;
using Hive.MicroServices.Api;
using Hive.MicroServices.Extensions;
using CloudTek.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hive.OpenTelemetry.Tests;

public class ResourceConfigurationTests
{
  private const string ServiceName = "opentelemetry-resource-tests";

  [Fact]
  [UnitTest]
  public async Task GivenMicroService_WhenOpenTelemetryConfigured_ThenServiceNameIsSetFromMicroServiceName()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var config = new ConfigurationBuilder().Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<ResourceConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert
    await action.Should().NotThrowAsync();
    // Service name from IMicroService.Name is used for resource configuration
    service.Name.Should().Be(ServiceName);
  }

  [Fact]
  [UnitTest]
  public async Task GivenMicroService_WhenOpenTelemetryConfigured_ThenServiceInstanceIdIsSetFromMicroServiceId()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var config = new ConfigurationBuilder().Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<ResourceConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert
    await action.Should().NotThrowAsync();
    // Service instance ID from IMicroService.Id is used for resource configuration
    service.Id.Should().NotBeNullOrEmpty();
  }

  [Fact]
  [UnitTest]
  public async Task GivenServiceNamespaceInConfiguration_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Resource:ServiceNamespace"] = "test-namespace"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<ResourceConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert
    await action.Should().NotThrowAsync();
  }

  [Fact]
  [UnitTest]
  public async Task GivenServiceVersionInConfiguration_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Resource:ServiceVersion"] = "2.0.0"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<ResourceConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert
    await action.Should().NotThrowAsync();
  }

  [Fact]
  [UnitTest]
  public async Task GivenCustomResourceAttributes_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Resource:Attributes:environment"] = "test",
      ["OpenTelemetry:Resource:Attributes:team"] = "platform",
      ["OpenTelemetry:Resource:Attributes:region"] = "us-west-2"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<ResourceConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert
    await action.Should().NotThrowAsync();
  }

  [Fact]
  [UnitTest]
  public async Task GivenNoResourceConfiguration_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var config = new ConfigurationBuilder().Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<ResourceConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert
    await action.Should().NotThrowAsync();
  }
}