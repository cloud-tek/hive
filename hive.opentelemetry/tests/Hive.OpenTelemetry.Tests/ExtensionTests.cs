using FluentAssertions;
using Hive.MicroServices;
using Hive.MicroServices.Extensions;
using Hive.Testing;
using Microsoft.Extensions.Logging.Abstractions;
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
  public void GivenWithOpenTelemetry_WhenCalledWithAdditionalSources_ThenExtensionIsAdded()
  {
    // Arrange
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());

    // Act
    var result = service.WithOpenTelemetry(
      additionalActivitySources: ["MyApp.Source1", "MyApp.Source2"]);

    // Assert
    service.Extensions.Should().HaveCount(1);
    service.Extensions.Should().ContainSingle(e => e is Extension);
    result.Should().BeSameAs(service);
  }

  [Fact]
  [UnitTest]
  public void GivenWithOpenTelemetry_WhenCalledWithNoParameters_ThenExtensionIsAdded()
  {
    // Arrange
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());

    // Act
    var result = service.WithOpenTelemetry();

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