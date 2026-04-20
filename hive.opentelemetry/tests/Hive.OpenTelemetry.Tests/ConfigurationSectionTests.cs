using CloudTek.Testing;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Exporter;
using Xunit;

namespace Hive.OpenTelemetry.Tests;

/// <summary>
/// Tests for OpenTelemetry configuration constants, section keys, and option defaults
/// </summary>
public class ConfigurationSectionTests
{
  #region Constants Value Tests

  [Fact]
  [UnitTest]
  public void GivenOtelExporterOtlpEndpoint_WhenAccessed_ThenHasCorrectValue()
  {
    // Assert
    Constants.Environment.OtelExporterOtlpEndpoint.Should().Be("OTEL_EXPORTER_OTLP_ENDPOINT");
  }

  [Fact]
  [UnitTest]
  public void GivenOtelLoggingExporterSection_WhenAccessed_ThenHasCorrectValue()
  {
    // Assert
    Constants.OtelLoggingExporterSection.Should().Be("OpenTelemetry:Logging");
  }

  [Fact]
  [UnitTest]
  public void GivenOtelTracingExporterSection_WhenAccessed_ThenHasCorrectValue()
  {
    // Assert
    Constants.OtelTracingExporterSection.Should().Be("OpenTelemetry:Tracing");
  }

  [Fact]
  [UnitTest]
  public void GivenOtelMetricsExporterSection_WhenAccessed_ThenHasCorrectValue()
  {
    // Assert
    Constants.OtelMetricsExporterSection.Should().Be("OpenTelemetry:Metrics");
  }

  [Fact]
  [UnitTest]
  public void GivenOpenTelemetryOptionsSectionKey_WhenAccessed_ThenHasCorrectValue()
  {
    // Assert
    OpenTelemetryOptions.SectionKey.Should().Be("OpenTelemetry");
  }

  #endregion

  #region OpenTelemetryOptions Default Value Tests

  [Fact]
  [UnitTest]
  public void GivenNewOpenTelemetryOptions_WhenCreated_ThenHasDefaultResourceOptions()
  {
    // Arrange & Act
    var options = new OpenTelemetryOptions();

    // Assert
    options.Resource.Should().NotBeNull();
    options.Resource.ServiceNamespace.Should().BeNull();
    options.Resource.ServiceVersion.Should().BeNull();
    options.Resource.Attributes.Should().NotBeNull().And.BeEmpty();
  }

  [Fact]
  [UnitTest]
  public void GivenNewOpenTelemetryOptions_WhenCreated_ThenHasDefaultLoggingOptions()
  {
    // Arrange & Act
    var options = new OpenTelemetryOptions();

    // Assert
    options.Logging.Should().NotBeNull();
    options.Logging.EnableConsoleExporter.Should().BeTrue();
    options.Logging.EnableOtlpExporter.Should().BeFalse();
  }

  [Fact]
  [UnitTest]
  public void GivenNewOpenTelemetryOptions_WhenCreated_ThenHasDefaultTracingOptions()
  {
    // Arrange & Act
    var options = new OpenTelemetryOptions();

    // Assert
    options.Tracing.Should().NotBeNull();
    options.Tracing.EnableAspNetCoreInstrumentation.Should().BeTrue();
    options.Tracing.EnableHttpClientInstrumentation.Should().BeTrue();
    options.Tracing.EnableOtlpExporter.Should().BeFalse();
  }

  [Fact]
  [UnitTest]
  public void GivenNewOpenTelemetryOptions_WhenCreated_ThenHasDefaultMetricsOptions()
  {
    // Arrange & Act
    var options = new OpenTelemetryOptions();

    // Assert
    options.Metrics.Should().NotBeNull();
    options.Metrics.EnableAspNetCoreInstrumentation.Should().BeTrue();
    options.Metrics.EnableHttpClientInstrumentation.Should().BeTrue();
    options.Metrics.EnableRuntimeInstrumentation.Should().BeTrue();
    options.Metrics.EnableOtlpExporter.Should().BeFalse();
  }

  [Fact]
  [UnitTest]
  public void GivenNewOpenTelemetryOptions_WhenCreated_ThenHasDefaultOtlpOptions()
  {
    // Arrange & Act
    var options = new OpenTelemetryOptions();

    // Assert
    options.Otlp.Should().NotBeNull();
    options.Otlp.Endpoint.Should().BeNull();
    options.Otlp.Protocol.Should().Be(OtlpExportProtocol.Grpc);
    options.Otlp.Headers.Should().NotBeNull().And.BeEmpty();
    options.Otlp.TimeoutMilliseconds.Should().Be(10000);
  }

  #endregion

  #region Individual Options Default Value Tests

  [Fact]
  [UnitTest]
  public void GivenNewResourceOptions_WhenCreated_ThenHasCorrectDefaults()
  {
    // Arrange & Act
    var options = new ResourceOptions();

    // Assert
    options.ServiceNamespace.Should().BeNull();
    options.ServiceVersion.Should().BeNull();
    options.Attributes.Should().NotBeNull().And.BeEmpty();
  }

  [Fact]
  [UnitTest]
  public void GivenNewLoggingOptions_WhenCreated_ThenHasCorrectDefaults()
  {
    // Arrange & Act
    var options = new LoggingOptions();

    // Assert
    options.EnableConsoleExporter.Should().BeTrue();
    options.EnableOtlpExporter.Should().BeFalse();
  }

  [Fact]
  [UnitTest]
  public void GivenNewTracingOptions_WhenCreated_ThenHasCorrectDefaults()
  {
    // Arrange & Act
    var options = new TracingOptions();

    // Assert
    options.EnableAspNetCoreInstrumentation.Should().BeTrue();
    options.EnableHttpClientInstrumentation.Should().BeTrue();
    options.EnableOtlpExporter.Should().BeFalse();
  }

  [Fact]
  [UnitTest]
  public void GivenNewMetricsOptions_WhenCreated_ThenHasCorrectDefaults()
  {
    // Arrange & Act
    var options = new MetricsOptions();

    // Assert
    options.EnableAspNetCoreInstrumentation.Should().BeTrue();
    options.EnableHttpClientInstrumentation.Should().BeTrue();
    options.EnableRuntimeInstrumentation.Should().BeTrue();
    options.EnableOtlpExporter.Should().BeFalse();
  }

  [Fact]
  [UnitTest]
  public void GivenNewOtlpOptions_WhenCreated_ThenHasCorrectDefaults()
  {
    // Arrange & Act
    var options = new OtlpOptions();

    // Assert
    options.Endpoint.Should().BeNull();
    options.Protocol.Should().Be(OtlpExportProtocol.Grpc);
    options.Headers.Should().NotBeNull().And.BeEmpty();
    options.TimeoutMilliseconds.Should().Be(10000);
  }

  #endregion

  #region Configuration Binding Tests

  [Fact]
  [UnitTest]
  public void GivenConfiguration_WhenBindingToOpenTelemetryOptions_ThenSectionKeyIsCorrect()
  {
    // Arrange
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Logging:EnableConsoleExporter"] = "false"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    // Act
    var section = config.GetSection(OpenTelemetryOptions.SectionKey);
    var options = new OpenTelemetryOptions();
    section.Bind(options);

    // Assert
    section.Exists().Should().BeTrue();
    options.Logging.EnableConsoleExporter.Should().BeFalse();
  }

  [Fact]
  [UnitTest]
  public void GivenConfiguration_WhenBindingResourceSection_ThenValuesAreBound()
  {
    // Arrange
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Resource:ServiceNamespace"] = "my-namespace",
      ["OpenTelemetry:Resource:ServiceVersion"] = "1.2.3",
      ["OpenTelemetry:Resource:Attributes:env"] = "production",
      ["OpenTelemetry:Resource:Attributes:team"] = "platform"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    // Act
    var section = config.GetSection(OpenTelemetryOptions.SectionKey);
    var options = new OpenTelemetryOptions();
    section.Bind(options);

    // Assert
    options.Resource.ServiceNamespace.Should().Be("my-namespace");
    options.Resource.ServiceVersion.Should().Be("1.2.3");
    options.Resource.Attributes.Should().HaveCount(2);
    options.Resource.Attributes["env"].Should().Be("production");
    options.Resource.Attributes["team"].Should().Be("platform");
  }

  [Fact]
  [UnitTest]
  public void GivenConfiguration_WhenBindingLoggingSection_ThenValuesAreBound()
  {
    // Arrange
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Logging:EnableConsoleExporter"] = "false",
      ["OpenTelemetry:Logging:EnableOtlpExporter"] = "true"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    // Act
    var section = config.GetSection(OpenTelemetryOptions.SectionKey);
    var options = new OpenTelemetryOptions();
    section.Bind(options);

    // Assert
    options.Logging.EnableConsoleExporter.Should().BeFalse();
    options.Logging.EnableOtlpExporter.Should().BeTrue();
  }

  [Fact]
  [UnitTest]
  public void GivenConfiguration_WhenBindingTracingSection_ThenValuesAreBound()
  {
    // Arrange
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Tracing:EnableAspNetCoreInstrumentation"] = "false",
      ["OpenTelemetry:Tracing:EnableHttpClientInstrumentation"] = "false",
      ["OpenTelemetry:Tracing:EnableOtlpExporter"] = "true"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    // Act
    var section = config.GetSection(OpenTelemetryOptions.SectionKey);
    var options = new OpenTelemetryOptions();
    section.Bind(options);

    // Assert
    options.Tracing.EnableAspNetCoreInstrumentation.Should().BeFalse();
    options.Tracing.EnableHttpClientInstrumentation.Should().BeFalse();
    options.Tracing.EnableOtlpExporter.Should().BeTrue();
  }

  [Fact]
  [UnitTest]
  public void GivenConfiguration_WhenBindingMetricsSection_ThenValuesAreBound()
  {
    // Arrange
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Metrics:EnableAspNetCoreInstrumentation"] = "false",
      ["OpenTelemetry:Metrics:EnableHttpClientInstrumentation"] = "false",
      ["OpenTelemetry:Metrics:EnableRuntimeInstrumentation"] = "false",
      ["OpenTelemetry:Metrics:EnableOtlpExporter"] = "true"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    // Act
    var section = config.GetSection(OpenTelemetryOptions.SectionKey);
    var options = new OpenTelemetryOptions();
    section.Bind(options);

    // Assert
    options.Metrics.EnableAspNetCoreInstrumentation.Should().BeFalse();
    options.Metrics.EnableHttpClientInstrumentation.Should().BeFalse();
    options.Metrics.EnableRuntimeInstrumentation.Should().BeFalse();
    options.Metrics.EnableOtlpExporter.Should().BeTrue();
  }

  [Fact]
  [UnitTest]
  public void GivenConfiguration_WhenBindingOtlpSection_ThenValuesAreBound()
  {
    // Arrange
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Otlp:Endpoint"] = "http://collector:4317",
      ["OpenTelemetry:Otlp:Protocol"] = "HttpProtobuf",
      ["OpenTelemetry:Otlp:TimeoutMilliseconds"] = "5000",
      ["OpenTelemetry:Otlp:Headers:x-api-key"] = "secret",
      ["OpenTelemetry:Otlp:Headers:Authorization"] = "Bearer token"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    // Act
    var section = config.GetSection(OpenTelemetryOptions.SectionKey);
    var options = new OpenTelemetryOptions();
    section.Bind(options);

    // Assert
    options.Otlp.Endpoint.Should().Be("http://collector:4317");
    options.Otlp.Protocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
    options.Otlp.TimeoutMilliseconds.Should().Be(5000);
    options.Otlp.Headers.Should().HaveCount(2);
    options.Otlp.Headers["x-api-key"].Should().Be("secret");
    options.Otlp.Headers["Authorization"].Should().Be("Bearer token");
  }

  [Fact]
  [UnitTest]
  public void GivenConfiguration_WhenBindingFullConfiguration_ThenAllValuesAreBound()
  {
    // Arrange
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Resource:ServiceNamespace"] = "test-ns",
      ["OpenTelemetry:Resource:ServiceVersion"] = "2.0.0",
      ["OpenTelemetry:Resource:Attributes:env"] = "staging",
      ["OpenTelemetry:Logging:EnableConsoleExporter"] = "false",
      ["OpenTelemetry:Logging:EnableOtlpExporter"] = "true",
      ["OpenTelemetry:Tracing:EnableAspNetCoreInstrumentation"] = "true",
      ["OpenTelemetry:Tracing:EnableHttpClientInstrumentation"] = "false",
      ["OpenTelemetry:Tracing:EnableOtlpExporter"] = "true",
      ["OpenTelemetry:Metrics:EnableAspNetCoreInstrumentation"] = "true",
      ["OpenTelemetry:Metrics:EnableHttpClientInstrumentation"] = "true",
      ["OpenTelemetry:Metrics:EnableRuntimeInstrumentation"] = "false",
      ["OpenTelemetry:Metrics:EnableOtlpExporter"] = "true",
      ["OpenTelemetry:Otlp:Endpoint"] = "http://otel:4317",
      ["OpenTelemetry:Otlp:Protocol"] = "Grpc",
      ["OpenTelemetry:Otlp:TimeoutMilliseconds"] = "15000"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    // Act
    var section = config.GetSection(OpenTelemetryOptions.SectionKey);
    var options = new OpenTelemetryOptions();
    section.Bind(options);

    // Assert - Resource
    options.Resource.ServiceNamespace.Should().Be("test-ns");
    options.Resource.ServiceVersion.Should().Be("2.0.0");
    options.Resource.Attributes["env"].Should().Be("staging");

    // Assert - Logging
    options.Logging.EnableConsoleExporter.Should().BeFalse();
    options.Logging.EnableOtlpExporter.Should().BeTrue();

    // Assert - Tracing
    options.Tracing.EnableAspNetCoreInstrumentation.Should().BeTrue();
    options.Tracing.EnableHttpClientInstrumentation.Should().BeFalse();
    options.Tracing.EnableOtlpExporter.Should().BeTrue();

    // Assert - Metrics
    options.Metrics.EnableAspNetCoreInstrumentation.Should().BeTrue();
    options.Metrics.EnableHttpClientInstrumentation.Should().BeTrue();
    options.Metrics.EnableRuntimeInstrumentation.Should().BeFalse();
    options.Metrics.EnableOtlpExporter.Should().BeTrue();

    // Assert - OTLP
    options.Otlp.Endpoint.Should().Be("http://otel:4317");
    options.Otlp.Protocol.Should().Be(OtlpExportProtocol.Grpc);
    options.Otlp.TimeoutMilliseconds.Should().Be(15000);
  }

  [Fact]
  [UnitTest]
  public void GivenEmptyConfiguration_WhenBindingToOptions_ThenDefaultsArePreserved()
  {
    // Arrange
    var config = new ConfigurationBuilder().Build();

    // Act
    var section = config.GetSection(OpenTelemetryOptions.SectionKey);
    var options = new OpenTelemetryOptions();
    section.Bind(options);

    // Assert - defaults should be preserved
    options.Logging.EnableConsoleExporter.Should().BeTrue();
    options.Logging.EnableOtlpExporter.Should().BeFalse();
    options.Tracing.EnableAspNetCoreInstrumentation.Should().BeTrue();
    options.Tracing.EnableHttpClientInstrumentation.Should().BeTrue();
    options.Metrics.EnableAspNetCoreInstrumentation.Should().BeTrue();
    options.Metrics.EnableHttpClientInstrumentation.Should().BeTrue();
    options.Metrics.EnableRuntimeInstrumentation.Should().BeTrue();
    options.Otlp.Protocol.Should().Be(OtlpExportProtocol.Grpc);
    options.Otlp.TimeoutMilliseconds.Should().Be(10000);
  }

  [Fact]
  [UnitTest]
  public void GivenPartialConfiguration_WhenBindingToOptions_ThenOnlySpecifiedValuesChange()
  {
    // Arrange
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Logging:EnableConsoleExporter"] = "false"
      // All other values not specified
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    // Act
    var section = config.GetSection(OpenTelemetryOptions.SectionKey);
    var options = new OpenTelemetryOptions();
    section.Bind(options);

    // Assert - only EnableConsoleExporter should change
    options.Logging.EnableConsoleExporter.Should().BeFalse();
    options.Logging.EnableOtlpExporter.Should().BeFalse(); // default preserved

    // All other defaults preserved
    options.Tracing.EnableAspNetCoreInstrumentation.Should().BeTrue();
    options.Tracing.EnableHttpClientInstrumentation.Should().BeTrue();
    options.Metrics.EnableRuntimeInstrumentation.Should().BeTrue();
    options.Otlp.TimeoutMilliseconds.Should().Be(10000);
  }

  #endregion

  #region Legacy Section Constants Tests

  [Fact]
  [UnitTest]
  public void GivenConfiguration_WhenUsingLoggingSectionConstant_ThenSectionCanBeAccessed()
  {
    // Arrange
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Logging:EnableConsoleExporter"] = "false"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    // Act
    var section = config.GetSection(Constants.OtelLoggingExporterSection);

    // Assert
    section.Exists().Should().BeTrue();
    section["EnableConsoleExporter"].Should().Be("false");
  }

  [Fact]
  [UnitTest]
  public void GivenConfiguration_WhenUsingTracingSectionConstant_ThenSectionCanBeAccessed()
  {
    // Arrange
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Tracing:EnableAspNetCoreInstrumentation"] = "false"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    // Act
    var section = config.GetSection(Constants.OtelTracingExporterSection);

    // Assert
    section.Exists().Should().BeTrue();
    section["EnableAspNetCoreInstrumentation"].Should().Be("false");
  }

  [Fact]
  [UnitTest]
  public void GivenConfiguration_WhenUsingMetricsSectionConstant_ThenSectionCanBeAccessed()
  {
    // Arrange
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Metrics:EnableRuntimeInstrumentation"] = "false"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    // Act
    var section = config.GetSection(Constants.OtelMetricsExporterSection);

    // Assert
    section.Exists().Should().BeTrue();
    section["EnableRuntimeInstrumentation"].Should().Be("false");
  }

  #endregion

  #region Protocol Enum Binding Tests

  [Fact]
  [UnitTest]
  public void GivenGrpcProtocolInConfiguration_WhenBinding_ThenProtocolIsGrpc()
  {
    // Arrange
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Otlp:Protocol"] = "Grpc"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    // Act
    var section = config.GetSection(OpenTelemetryOptions.SectionKey);
    var options = new OpenTelemetryOptions();
    section.Bind(options);

    // Assert
    options.Otlp.Protocol.Should().Be(OtlpExportProtocol.Grpc);
  }

  [Fact]
  [UnitTest]
  public void GivenHttpProtobufProtocolInConfiguration_WhenBinding_ThenProtocolIsHttpProtobuf()
  {
    // Arrange
    var configJson = new Dictionary<string, string>
    {
      ["OpenTelemetry:Otlp:Protocol"] = "HttpProtobuf"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configJson!)
      .Build();

    // Act
    var section = config.GetSection(OpenTelemetryOptions.SectionKey);
    var options = new OpenTelemetryOptions();
    section.Bind(options);

    // Assert
    options.Otlp.Protocol.Should().Be(OtlpExportProtocol.HttpProtobuf);
  }

  #endregion
}