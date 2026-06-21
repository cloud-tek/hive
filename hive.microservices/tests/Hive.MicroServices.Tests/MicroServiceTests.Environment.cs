using System.Threading.Tasks;
using CloudTek.Testing;
using FluentAssertions;
using Hive.Exceptions;
using Hive.MicroServices.Extensions;
using Hive.MicroServices.Testing;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Hive.MicroServices.Tests;

public partial class MicroServiceTests
{
  public class Environment
  {
    private const string ServiceName = "microservice-tests-environment";

    [Fact]
    [UnitTest]
    public async Task GivenOnlyAspNetCoreEnvironmentSet_WhenInitializeAsync_ThenNoConflictThrown()
    {
      // Arrange — DOTNET_ENVIRONMENT cleared (empty) so only ASPNETCORE_ENVIRONMENT is effective
      using var aspScope = EnvironmentVariableScope.Create(Constants.EnvironmentVariables.DotNet.Environment, "Development");
      using var dotScope = EnvironmentVariableScope.Create(Constants.EnvironmentVariables.DotNet.RuntimeEnvironment, "");

      var config = new ConfigurationBuilder().Build();
      var service = new MicroService(ServiceName)
        .InTestClass<MicroServiceTests>()
        .ConfigureTestHost();

      // Act
      var action = async () => await service.InitializeAsync(config);

      // Assert
      await action.Should().NotThrowAsync<ConfigurationException>();
    }

    [Fact]
    [UnitTest]
    public async Task GivenOnlyDotNetEnvironmentSet_WhenInitializeAsync_ThenNoConflictThrown()
    {
      // Arrange — ASPNETCORE_ENVIRONMENT cleared (empty) so only DOTNET_ENVIRONMENT is effective
      using var aspScope = EnvironmentVariableScope.Create(Constants.EnvironmentVariables.DotNet.Environment, "");
      using var dotScope = EnvironmentVariableScope.Create(Constants.EnvironmentVariables.DotNet.RuntimeEnvironment, "Development");

      var config = new ConfigurationBuilder().Build();
      var service = new MicroService(ServiceName)
        .InTestClass<MicroServiceTests>()
        .ConfigureTestHost();

      // Act
      var action = async () => await service.InitializeAsync(config);

      // Assert
      await action.Should().NotThrowAsync<ConfigurationException>();
    }

    [Fact]
    [UnitTest]
    public async Task GivenBothSetToSameValueDifferentCasing_WhenInitializeAsync_ThenNoConflictThrown()
    {
      // Arrange — same environment, different case
      using var aspScope = EnvironmentVariableScope.Create(Constants.EnvironmentVariables.DotNet.Environment, "Development");
      using var dotScope = EnvironmentVariableScope.Create(Constants.EnvironmentVariables.DotNet.RuntimeEnvironment, "development");

      var config = new ConfigurationBuilder().Build();
      var service = new MicroService(ServiceName)
        .InTestClass<MicroServiceTests>()
        .ConfigureTestHost();

      // Act
      var action = async () => await service.InitializeAsync(config);

      // Assert
      await action.Should().NotThrowAsync<ConfigurationException>();
    }

    [Fact]
    [UnitTest]
    public async Task GivenBothSetToDifferentValues_WhenInitializeAsync_ThenThrowsConfigurationExceptionNamingBothVariables()
    {
      // Arrange — genuinely conflicting environment values
      using var aspScope = EnvironmentVariableScope.Create(Constants.EnvironmentVariables.DotNet.Environment, "Development");
      using var dotScope = EnvironmentVariableScope.Create(Constants.EnvironmentVariables.DotNet.RuntimeEnvironment, "Production");

      var service = new MicroService(ServiceName)
        .InTestClass<MicroServiceTests>();

      // Act & Assert
      var ex = await service.Invoking(s => s.InitializeAsync())
        .Should().ThrowAsync<ConfigurationException>();

      ex.And.Message.Should().Contain("ASPNETCORE_ENVIRONMENT");
      ex.And.Message.Should().Contain("DOTNET_ENVIRONMENT");
    }
  }
}