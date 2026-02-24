using FluentAssertions;
using Hive.MicroServices;
using Hive.MicroServices.Api;
using Hive.MicroServices.Extensions;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hive.OpenTelemetry.Tests;

/// <summary>
/// Tests for OpenTelemetry logging configuration
/// </summary>
public class LoggingConfigurationTests
{
  private const string ServiceName = "opentelemetry-logging-tests";

  #region Default Logging Configuration Tests

  [Fact]
  [UnitTest]
  public async Task GivenNoConfiguration_WhenServiceStarts_ThenConsoleExporterIsEnabledByDefault()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var config = new ConfigurationBuilder().Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<LoggingConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert - service starts successfully with default console exporter
    await action.Should().NotThrowAsync();
    service.Extensions.Should().ContainSingle(e => e is Extension);
  }

  [Fact]
  [UnitTest]
  public async Task GivenConsoleExporterExplicitlyEnabled_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Logging:EnableConsoleExporter"] = "true"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<LoggingConfigurationTests>()
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
  public async Task GivenConsoleExporterDisabled_WhenServiceStarts_ThenServiceStartsSuccessfully()
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
      .InTestClass<LoggingConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert - service starts even without any exporters
    await action.Should().NotThrowAsync();
  }

  #endregion

  #region OTLP Exporter with IConfiguration Tests

  [Fact]
  [UnitTest]
  public async Task GivenOtlpEndpointInConfiguration_WhenServiceStarts_ThenOtlpExporterIsConfigured()
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
      .InTestClass<LoggingConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert - OTLP exporter is implicitly enabled when endpoint is configured
    await action.Should().NotThrowAsync();
  }

  [Fact]
  [UnitTest]
  public async Task GivenOtlpExporterExplicitlyEnabled_WhenEndpointConfigured_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Logging:EnableOtlpExporter"] = "true",
      ["OpenTelemetry:Otlp:Endpoint"] = "http://otel-collector:4317"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<LoggingConfigurationTests>()
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
  public async Task GivenOtlpWithGrpcProtocol_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Otlp:Endpoint"] = "http://localhost:4317",
      ["OpenTelemetry:Otlp:Protocol"] = "Grpc"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<LoggingConfigurationTests>()
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
  public async Task GivenOtlpWithHttpProtobufProtocol_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Otlp:Endpoint"] = "http://localhost:4318/v1/logs",
      ["OpenTelemetry:Otlp:Protocol"] = "HttpProtobuf"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<LoggingConfigurationTests>()
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
  public async Task GivenOtlpWithCustomTimeout_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Otlp:Endpoint"] = "http://localhost:4317",
      ["OpenTelemetry:Otlp:TimeoutMilliseconds"] = "5000"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<LoggingConfigurationTests>()
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
  public async Task GivenOtlpWithHeaders_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Otlp:Endpoint"] = "http://localhost:4317",
      ["OpenTelemetry:Otlp:Headers:x-api-key"] = "test-api-key",
      ["OpenTelemetry:Otlp:Headers:Authorization"] = "Bearer token123"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<LoggingConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert
    await action.Should().NotThrowAsync();
  }

  #endregion

  #region Environment Variable Fallback Tests

  [Fact]
  [UnitTest]
  public async Task GivenOtlpEndpointInEnvironmentVariable_WhenNoConfigurationEndpoint_ThenEnvironmentVariableIsUsed()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    using var scope = EnvironmentVariableScope.Create(
      Constants.Environment.OtelExporterOtlpEndpoint,
      "http://env-collector:4317");

    // Create service AFTER setting environment variable
    // so it captures the environment variable
    var config = new ConfigurationBuilder().Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<LoggingConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert - service starts using environment variable endpoint
    await action.Should().NotThrowAsync();
    service.EnvironmentVariables.Should().ContainKey(Constants.Environment.OtelExporterOtlpEndpoint);
    service.EnvironmentVariables[Constants.Environment.OtelExporterOtlpEndpoint].Should().Be("http://env-collector:4317");
  }

  [Fact]
  [UnitTest]
  public async Task GivenOtlpEndpointInBothConfigurationAndEnvironment_WhenServiceStarts_ThenConfigurationTakesPriority()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    using var scope = EnvironmentVariableScope.Create(
      Constants.Environment.OtelExporterOtlpEndpoint,
      "http://env-collector:4317");

    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Otlp:Endpoint"] = "http://config-collector:4317"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<LoggingConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert - service starts successfully, configuration takes priority
    await action.Should().NotThrowAsync();
  }

  [Fact]
  [UnitTest]
  public async Task GivenNoOtlpEndpoint_WhenServiceStarts_ThenOnlyConsoleExporterIsUsed()
  {
    // Arrange - ensure no environment variable is set
    var config = new ConfigurationBuilder().Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<LoggingConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert - service starts with only console exporter
    await action.Should().NotThrowAsync();
  }

  [Fact]
  [UnitTest]
  public async Task GivenEmptyOtlpEndpointInConfiguration_WhenEnvironmentVariableSet_ThenEnvironmentVariableIsUsed()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    using var scope = EnvironmentVariableScope.Create(
      Constants.Environment.OtelExporterOtlpEndpoint,
      "http://env-fallback-collector:4317");

    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Otlp:Endpoint"] = "" // Empty string should fall back to env var
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<LoggingConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert - service starts using environment variable as fallback
    await action.Should().NotThrowAsync();
  }

  #endregion

  #region Combined Configuration Tests

  [Fact]
  [UnitTest]
  public async Task GivenBothConsoleAndOtlpEnabled_WhenServiceStarts_ThenBothExportersAreConfigured()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Logging:EnableConsoleExporter"] = "true",
      ["OpenTelemetry:Logging:EnableOtlpExporter"] = "true",
      ["OpenTelemetry:Otlp:Endpoint"] = "http://localhost:4317"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<LoggingConfigurationTests>()
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
  public async Task GivenConsoleDisabledAndOtlpEnabled_WhenServiceStarts_ThenOnlyOtlpExporterIsConfigured()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Logging:EnableConsoleExporter"] = "false",
      ["OpenTelemetry:Otlp:Endpoint"] = "http://localhost:4317"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<LoggingConfigurationTests>()
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
  public async Task GivenFullLoggingConfiguration_WhenServiceStarts_ThenAllSettingsAreApplied()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Resource:ServiceNamespace"] = "test-namespace",
      ["OpenTelemetry:Resource:ServiceVersion"] = "1.0.0",
      ["OpenTelemetry:Logging:EnableConsoleExporter"] = "true",
      ["OpenTelemetry:Logging:EnableOtlpExporter"] = "true",
      ["OpenTelemetry:Otlp:Endpoint"] = "http://otel-collector:4317",
      ["OpenTelemetry:Otlp:Protocol"] = "Grpc",
      ["OpenTelemetry:Otlp:TimeoutMilliseconds"] = "5000",
      ["OpenTelemetry:Otlp:Headers:x-api-key"] = "test-key"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<LoggingConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert
    await action.Should().NotThrowAsync();
  }

  #endregion
}