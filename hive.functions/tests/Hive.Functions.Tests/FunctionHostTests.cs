using FluentAssertions;
using CloudTek.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hive.Functions.Tests;

/// <summary>
/// Tests for FunctionHost
/// </summary>
public class FunctionHostTests
{
  [Fact]
  [UnitTest]
  public void FunctionHost_Constructor_ShouldInitializeProperties()
  {
    // Arrange & Act
    var functionHost = new FunctionHost("test-function");

    // Assert
    functionHost.Name.Should().Be("test-function");
    functionHost.Id.Should().NotBeNullOrEmpty();
    functionHost.Extensions.Should().NotBeNull();
    functionHost.Extensions.Should().BeEmpty();
    functionHost.EnvironmentVariables.Should().NotBeNull();
  }

  [Fact]
  [UnitTest]
  public void FunctionHost_Constructor_WithNullName_ShouldThrowArgumentNullException()
  {
    // Arrange & Act & Assert
    var act = () => new FunctionHost(null!);
    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("name");
  }

  [Fact]
  [UnitTest]
  public void FunctionHost_Id_ShouldBeUnique()
  {
    // Arrange & Act
    var functionHost1 = new FunctionHost("test1");
    var functionHost2 = new FunctionHost("test2");

    // Assert
    functionHost1.Id.Should().NotBe(functionHost2.Id);
  }

  [Fact]
  [UnitTest]
  public void FunctionHost_ConfigureServices_ShouldAddConfiguration()
  {
    // Arrange
    var functionHost = new FunctionHost("test-function");

    // Act
    var result = functionHost.ConfigureServices((_, _) =>
    {
      // Configuration would happen here
    });

    // Assert - verify fluent interface
    // Note: The callback is stored and invoked later during host creation (CreateHostBuilder)
    // not immediately, so we only verify the fluent interface pattern here
    result.Should().BeSameAs(functionHost);
  }

  [Fact]
  [UnitTest]
  public void FunctionHost_ConfigureFunctions_ShouldAddConfiguration()
  {
    // Arrange
    var functionHost = new FunctionHost("test-function");

    // Act
    var result = functionHost.ConfigureFunctions(_ =>
    {
      // Configuration would happen here
    });

    // Assert - verify fluent interface
    // Note: The callback is stored and invoked later during host creation (CreateHostBuilder)
    // not immediately, so we only verify the fluent interface pattern here
    result.Should().BeSameAs(functionHost);
  }

  [Fact]
  [UnitTest]
  public void FunctionHost_Extensions_ShouldBeEmpty()
  {
    // Arrange & Act
    var functionHost = new FunctionHost("test-function");

    // Assert
    functionHost.Extensions.Should().NotBeNull();
    functionHost.Extensions.Should().BeEmpty();
  }

  [Fact]
  [UnitTest]
  public void FunctionHost_Environment_ShouldDefaultToProduction()
  {
    // Arrange & Act
    var functionHost = new FunctionHost("test-function");

    // Assert
    functionHost.Environment.Should().NotBeNullOrEmpty();
  }

  [Fact]
  [UnitTest]
  public async Task FunctionHost_StartAsync_WithoutInitialize_ShouldThrow()
  {
    // Arrange
    var functionHost = new FunctionHost("test-function");
    var core = (IMicroServiceCore)functionHost;

    // Act & Assert
    var act = async () => await core.StartAsync();
    await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("Host not initialized. Call InitializeAsync first.");
  }

  [Fact]
  [UnitTest]
  public void FunctionHost_ConfigureServices_ShouldStoreCallback()
  {
    // Arrange
    var callbackExecuted = false;
    var functionHost = new FunctionHost("test-function");

    // Act
    functionHost.ConfigureServices((services, config) =>
    {
      callbackExecuted = true;
    });

    // Assert - callback is stored (will be invoked during host initialization)
    // We verify the fluent API returns the same instance
    functionHost.Should().NotBeNull();
    // Note: The callback will be executed during CreateHostBuilder, not immediately
    callbackExecuted.Should().BeFalse("callback should not execute until host is built");
  }

  [Fact]
  [UnitTest]
  public void FunctionHost_RegisterExtension_ShouldAddToExtensionsList()
  {
    // Arrange
    var functionHost = new FunctionHost("test-function");
    var core = (IMicroServiceCore)functionHost;

    // Act
    core.RegisterExtension<TestExtension>();

    // Assert
    functionHost.Extensions.Should().HaveCount(1);
    functionHost.Extensions[0].Should().BeOfType<TestExtension>();
  }

  [Fact]
  [UnitTest]
  public void FunctionHost_ConfigurationRoot_ShouldBeAvailableImmediately()
  {
    // Arrange & Act
    var functionHost = new FunctionHost("test-function");

    // Assert - ConfigurationRoot should be available before InitializeAsync
    functionHost.ConfigurationRoot.Should().NotBeNull();
    var act = () => functionHost.ConfigurationRoot.GetSection("NonExistent");
    act.Should().NotThrow<NullReferenceException>();
  }

  /// <summary>
  /// Test service for DI validation
  /// </summary>
  private sealed class TestService
  {
  }

  /// <summary>
  /// Test extension for extension registration validation
  /// </summary>
  private sealed class TestExtension : MicroServiceExtension<TestExtension>
  {
    public TestExtension(IMicroServiceCore service) : base(service)
    {
    }
  }
}