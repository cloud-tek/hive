using FluentAssertions;
using Hive.MicroServices;
using Hive.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Xunit;

namespace Hive.OpenTelemetry.Tests;

public class ExtensionTests
{
  private const string ServiceName = "opentelemetry-extension-tests";

  [Fact]
  [UnitTest]
  public void GivenWithOpenTelemetry_WhenCalled_ThenExtensionIsAddedToServiceExtensions()
  {
    // Arrange
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());

    // Act
    var result = service.WithOpenTelemetry();

    // Assert
    service.Extensions.Should().HaveCount(1);
    service.Extensions.Should().ContainSingle(e => e is global::Hive.OpenTelemetry.Extension);
  }

  [Fact]
  [UnitTest]
  public void GivenWithOpenTelemetry_WhenCalled_ThenReturnsIMicroServiceForChaining()
  {
    // Arrange
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());

    // Act
    var result = service.WithOpenTelemetry();

    // Assert
    result.Should().BeSameAs(service);
  }

  [Fact]
  [UnitTest]
  public void GivenWithOpenTelemetry_WhenCalledWithCustomLogging_ThenExtensionIsAdded()
  {
    // Arrange
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());

    // Act
    var result = service.WithOpenTelemetry(
      logging: builder =>
      {
        // Custom logging configuration
      });

    // Assert
    service.Extensions.Should().HaveCount(1);
    service.Extensions.Should().ContainSingle(e => e is Extension);
    result.Should().BeSameAs(service);
  }

  [Fact]
  [UnitTest]
  public void GivenWithOpenTelemetry_WhenCalledWithCustomTracing_ThenExtensionIsAdded()
  {
    // Arrange
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());

    // Act
    var result = service.WithOpenTelemetry(
      tracing: builder =>
      {
        // Custom tracing configuration
      });

    // Assert
    service.Extensions.Should().HaveCount(1);
    service.Extensions.Should().ContainSingle(e => e is Extension);
    result.Should().BeSameAs(service);
  }

  [Fact]
  [UnitTest]
  public void GivenWithOpenTelemetry_WhenCalledWithCustomMetrics_ThenExtensionIsAdded()
  {
    // Arrange
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());

    // Act
    var result = service.WithOpenTelemetry(
      metrics: builder =>
      {
        // Custom metrics configuration
      });

    // Assert
    service.Extensions.Should().HaveCount(1);
    service.Extensions.Should().ContainSingle(e => e is Extension);
    result.Should().BeSameAs(service);
  }

  [Fact]
  [UnitTest]
  public void GivenWithOpenTelemetry_WhenCalledWithAllCustomConfigurations_ThenExtensionIsAdded()
  {
    // Arrange
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());

    // Act
    var result = service.WithOpenTelemetry(
      logging: builder =>
      {
        // Custom logging configuration
      },
      tracing: builder =>
      {
        // Custom tracing configuration
      },
      metrics: builder =>
      {
        // Custom metrics configuration
      });

    // Assert
    service.Extensions.Should().HaveCount(1);
    service.Extensions.Should().ContainSingle(e => e is Extension);
    result.Should().BeSameAs(service);
  }

  [Fact]
  [UnitTest]
  public void GivenWithOpenTelemetry_WhenCalledWithCustomEnvironmentVariable_ThenExtensionIsAdded()
  {
    // Arrange
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());
    var customEnvVar = "CUSTOM_OTLP_ENDPOINT";

    // Act
    var result = service.WithOpenTelemetry(
      otelExporterOtlpEnvpointEnvVar: customEnvVar);

    // Assert
    service.Extensions.Should().HaveCount(1);
    service.Extensions.Should().ContainSingle(e => e is Extension);
    result.Should().BeSameAs(service);
  }

  [Fact]
  [UnitTest]
  public void GivenWithOpenTelemetry_WhenCalledMultipleTimes_ThenMultipleExtensionsAreAdded()
  {
    // Arrange
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());

    // Act
    service.WithOpenTelemetry();
    service.WithOpenTelemetry();

    // Assert
    service.Extensions.Should().HaveCount(2);
    service.Extensions.Should().AllBeOfType<Extension>();
  }

  [Fact]
  [UnitTest]
  public void GivenWithOpenTelemetry_WhenChainedWithOtherExtensions_ThenAllExtensionsAreAdded()
  {
    // Arrange & Act
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
      .WithOpenTelemetry()
      .ConfigureServices((services, config) => { });

    // Assert
    service.Extensions.Should().HaveCount(1);
    service.Extensions.Should().ContainSingle(e => e is Extension);
  }
}