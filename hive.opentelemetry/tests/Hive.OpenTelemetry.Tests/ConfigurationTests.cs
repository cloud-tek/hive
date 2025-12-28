using FluentAssertions;
using Hive.MicroServices;
using Hive.MicroServices.Api;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hive.OpenTelemetry.Tests;

public class ConfigurationTests
{
  private const string ServiceName = "opentelemetry-configuration-tests";

  [Fact]
  [UnitTest]
  public async Task GivenNoConfiguration_WhenServiceStarts_ThenUsesDefaultOptions()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var config = new ConfigurationBuilder().Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<ConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert
    await action.Should().NotThrowAsync();
    service.Extensions.Should().ContainSingle(e => e is Extension);
  }

  [Fact]
  [UnitTest]
  public async Task GivenConfigurationInJson_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Resource:ServiceNamespace"] = "test-namespace",
      ["OpenTelemetry:Resource:ServiceVersion"] = "2.0.0",
      ["OpenTelemetry:Resource:Attributes:environment"] = "test",
      ["OpenTelemetry:Logging:EnableConsoleExporter"] = "false",
      ["OpenTelemetry:Tracing:EnableAspNetCoreInstrumentation"] = "false",
      ["OpenTelemetry:Metrics:EnableRuntimeInstrumentation"] = "false",
      ["OpenTelemetry:Otlp:Endpoint"] = "http://otel-collector:4317",
      ["OpenTelemetry:Otlp:Protocol"] = "Grpc",
      ["OpenTelemetry:Otlp:TimeoutMilliseconds"] = "5000"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<ConfigurationTests>()
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
  public async Task GivenOtlpEndpointInConfiguration_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Otlp:Endpoint"] = "http://localhost:4317"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<ConfigurationTests>()
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
  public async Task GivenPartialConfiguration_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Logging:EnableConsoleExporter"] = "false"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<ConfigurationTests>()
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
  public async Task GivenMultipleResourceAttributes_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Resource:Attributes:attr1"] = "value1",
      ["OpenTelemetry:Resource:Attributes:attr2"] = "value2",
      ["OpenTelemetry:Resource:Attributes:attr3"] = "value3"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<ConfigurationTests>()
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
  public async Task GivenOtlpHeaders_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Otlp:Endpoint"] = "http://localhost:4317",
      ["OpenTelemetry:Otlp:Headers:x-api-key"] = "secret",
      ["OpenTelemetry:Otlp:Headers:x-custom-header"] = "custom-value"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<ConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert
    await action.Should().NotThrowAsync();
  }
}
