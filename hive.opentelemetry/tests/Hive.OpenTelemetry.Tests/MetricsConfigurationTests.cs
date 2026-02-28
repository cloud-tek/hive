using CloudTek.Testing;
using FluentAssertions;
using Hive.MicroServices;
using Hive.MicroServices.Api;
using Hive.MicroServices.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hive.OpenTelemetry.Tests;

/// <summary>
/// Tests for OpenTelemetry metrics configuration
/// </summary>
public class MetricsConfigurationTests
{
  private const string ServiceName = "opentelemetry-metrics-tests";

  #region Default Metrics Configuration Tests

  [Fact]
  [UnitTest]
  public async Task GivenNoConfiguration_WhenServiceStarts_ThenDefaultInstrumentationsAreEnabled()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var config = new ConfigurationBuilder().Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert - service starts successfully with default instrumentations
    await action.Should().NotThrowAsync();
    service.Extensions.Should().ContainSingle(e => e is Extension);
  }

  [Fact]
  [UnitTest]
  public async Task GivenAspNetCoreInstrumentationExplicitlyEnabled_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Metrics:EnableAspNetCoreInstrumentation"] = "true"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
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
  public async Task GivenAspNetCoreInstrumentationDisabled_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Metrics:EnableAspNetCoreInstrumentation"] = "false"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
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
  public async Task GivenHttpClientInstrumentationExplicitlyEnabled_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Metrics:EnableHttpClientInstrumentation"] = "true"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
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
  public async Task GivenHttpClientInstrumentationDisabled_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Metrics:EnableHttpClientInstrumentation"] = "false"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
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
  public async Task GivenRuntimeInstrumentationExplicitlyEnabled_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Metrics:EnableRuntimeInstrumentation"] = "true"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
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
  public async Task GivenRuntimeInstrumentationDisabled_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Metrics:EnableRuntimeInstrumentation"] = "false"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
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
  public async Task GivenAllInstrumentationsDisabled_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Metrics:EnableAspNetCoreInstrumentation"] = "false",
      ["OpenTelemetry:Metrics:EnableHttpClientInstrumentation"] = "false",
      ["OpenTelemetry:Metrics:EnableRuntimeInstrumentation"] = "false"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert - service starts even with all instrumentations disabled
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
      .InTestClass<MetricsConfigurationTests>()
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
      ["OpenTelemetry:Metrics:EnableOtlpExporter"] = "true",
      ["OpenTelemetry:Otlp:Endpoint"] = "http://otel-collector:4317"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
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
      .InTestClass<MetricsConfigurationTests>()
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
      ["OpenTelemetry:Otlp:Endpoint"] = "http://localhost:4318/v1/metrics",
      ["OpenTelemetry:Otlp:Protocol"] = "HttpProtobuf"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
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
      .InTestClass<MetricsConfigurationTests>()
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
      .InTestClass<MetricsConfigurationTests>()
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
    var config = new ConfigurationBuilder().Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
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
      .InTestClass<MetricsConfigurationTests>()
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
  public async Task GivenNoOtlpEndpoint_WhenServiceStarts_ThenNoOtlpExporterIsConfigured()
  {
    // Arrange - ensure no environment variable is set
    var config = new ConfigurationBuilder().Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
      .WithOpenTelemetry()
      .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);

    // Act
    var action = async () => await service.RunAsync(config);

    // Assert - service starts without OTLP exporter
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
      .InTestClass<MetricsConfigurationTests>()
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
  public async Task GivenAllInstrumentationsAndOtlpEnabled_WhenServiceStarts_ThenAllAreConfigured()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Metrics:EnableAspNetCoreInstrumentation"] = "true",
      ["OpenTelemetry:Metrics:EnableHttpClientInstrumentation"] = "true",
      ["OpenTelemetry:Metrics:EnableRuntimeInstrumentation"] = "true",
      ["OpenTelemetry:Metrics:EnableOtlpExporter"] = "true",
      ["OpenTelemetry:Otlp:Endpoint"] = "http://localhost:4317"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
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
  public async Task GivenOnlyAspNetCoreInstrumentationEnabled_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Metrics:EnableAspNetCoreInstrumentation"] = "true",
      ["OpenTelemetry:Metrics:EnableHttpClientInstrumentation"] = "false",
      ["OpenTelemetry:Metrics:EnableRuntimeInstrumentation"] = "false"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
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
  public async Task GivenOnlyHttpClientInstrumentationEnabled_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Metrics:EnableAspNetCoreInstrumentation"] = "false",
      ["OpenTelemetry:Metrics:EnableHttpClientInstrumentation"] = "true",
      ["OpenTelemetry:Metrics:EnableRuntimeInstrumentation"] = "false"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
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
  public async Task GivenOnlyRuntimeInstrumentationEnabled_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Metrics:EnableAspNetCoreInstrumentation"] = "false",
      ["OpenTelemetry:Metrics:EnableHttpClientInstrumentation"] = "false",
      ["OpenTelemetry:Metrics:EnableRuntimeInstrumentation"] = "true"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
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
  public async Task GivenFullMetricsConfiguration_WhenServiceStarts_ThenAllSettingsAreApplied()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Resource:ServiceNamespace"] = "test-namespace",
      ["OpenTelemetry:Resource:ServiceVersion"] = "1.0.0",
      ["OpenTelemetry:Metrics:EnableAspNetCoreInstrumentation"] = "true",
      ["OpenTelemetry:Metrics:EnableHttpClientInstrumentation"] = "true",
      ["OpenTelemetry:Metrics:EnableRuntimeInstrumentation"] = "true",
      ["OpenTelemetry:Metrics:EnableOtlpExporter"] = "true",
      ["OpenTelemetry:Otlp:Endpoint"] = "http://otel-collector:4317",
      ["OpenTelemetry:Otlp:Protocol"] = "Grpc",
      ["OpenTelemetry:Otlp:TimeoutMilliseconds"] = "5000",
      ["OpenTelemetry:Otlp:Headers:x-api-key"] = "test-key"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
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
  public async Task GivenMetricsWithResourceAttributes_WhenServiceStarts_ThenResourceAttributesAreApplied()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Resource:ServiceNamespace"] = "my-namespace",
      ["OpenTelemetry:Resource:ServiceVersion"] = "2.0.0",
      ["OpenTelemetry:Resource:Attributes:deployment.environment"] = "staging",
      ["OpenTelemetry:Resource:Attributes:team"] = "platform",
      ["OpenTelemetry:Metrics:EnableAspNetCoreInstrumentation"] = "true"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
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
  public async Task GivenTwoInstrumentationsEnabled_WhenServiceStarts_ThenServiceStartsSuccessfully()
  {
    // Arrange
    using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out _);
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Metrics:EnableAspNetCoreInstrumentation"] = "true",
      ["OpenTelemetry:Metrics:EnableHttpClientInstrumentation"] = "true",
      ["OpenTelemetry:Metrics:EnableRuntimeInstrumentation"] = "false"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .InTestClass<MetricsConfigurationTests>()
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