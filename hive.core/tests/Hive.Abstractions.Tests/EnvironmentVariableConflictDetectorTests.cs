using CloudTek.Testing;
using FluentAssertions;
using Xunit;

namespace Hive.Abstractions.Tests;

public class EnvironmentVariableConflictDetectorTests
{
  [Fact]
  [UnitTest]
  public void GivenBothNull_WhenDetect_ThenReturnsNull()
  {
    // Act
    var result = EnvironmentVariableConflictDetector.Detect(null, null);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  [UnitTest]
  public void GivenOnlyAspNetCoreEnvironmentSet_WhenDetect_ThenReturnsNull()
  {
    // Act
    var result = EnvironmentVariableConflictDetector.Detect("Development", null);

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  [UnitTest]
  public void GivenOnlyDotNetEnvironmentSet_WhenDetect_ThenReturnsNull()
  {
    // Act
    var result = EnvironmentVariableConflictDetector.Detect(null, "Development");

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  [UnitTest]
  public void GivenBothSetToSameValue_WhenDetect_ThenReturnsNull()
  {
    // Act
    var result = EnvironmentVariableConflictDetector.Detect("Development", "Development");

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  [UnitTest]
  public void GivenBothSetToSameValueDifferentCasing_WhenDetect_ThenReturnsNull()
  {
    // Act
    var result = EnvironmentVariableConflictDetector.Detect("Development", "development");

    // Assert
    result.Should().BeNull();
  }

  [Fact]
  [UnitTest]
  public void GivenBothSetToDifferentValues_WhenDetect_ThenReturnsMessageNamingBothVariables()
  {
    // Act
    var result = EnvironmentVariableConflictDetector.Detect("Development", "Production");

    // Assert
    result.Should().NotBeNull();
    result.Should().Contain("ASPNETCORE_ENVIRONMENT");
    result.Should().Contain("DOTNET_ENVIRONMENT");
    result.Should().Contain("Development");
    result.Should().Contain("Production");
  }

  [Fact]
  [UnitTest]
  public void GivenBothSetToDifferentValues_WhenDetect_ThenMessageMentionsHiveHonorsAspNetCoreEnvironment()
  {
    // Act
    var result = EnvironmentVariableConflictDetector.Detect("Development", "Production");

    // Assert
    result.Should().Contain("ASPNETCORE_ENVIRONMENT");
  }

  [Theory]
  [UnitTest]
  [InlineData("", "Production")]
  [InlineData("Development", "")]
  [InlineData("", "")]
  public void GivenEmptyValues_WhenDetect_ThenReturnsNull(string? aspNetCore, string? dotNet)
  {
    // Act
    var result = EnvironmentVariableConflictDetector.Detect(aspNetCore, dotNet);

    // Assert
    result.Should().BeNull();
  }
}