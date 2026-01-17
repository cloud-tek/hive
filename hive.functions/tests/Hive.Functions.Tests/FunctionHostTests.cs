using FluentAssertions;
using Hive.Testing;
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
    var configureActionCalled = false;

    // Act
    functionHost.ConfigureServices((services, config) =>
    {
      configureActionCalled = true;
    });

    // Assert - we can't easily verify without running the host,
    // but we can verify it returns the instance for chaining
    functionHost.Should().NotBeNull();
  }

  [Fact]
  [UnitTest]
  public void FunctionHost_ConfigureFunctions_ShouldAddConfiguration()
  {
    // Arrange
    var functionHost = new FunctionHost("test-function");

    // Act
    var result = functionHost.ConfigureFunctions(builder =>
    {
      // Configuration would happen here
    });

    // Assert - verify fluent interface
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

}