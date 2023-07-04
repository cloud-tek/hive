using System;
using System.Threading.Tasks;
using FluentAssertions;
using Hive.Logging;
using Hive.Logging.Xunit;
using Hive.MicroServices.CORS;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Hive.MicroServices.Tests;

public partial class MicroServiceTests
{
  public class CORS
  {
    private const string ServiceName = "microservice-tests-cors";
    private ITestOutputHelper _output;
    public CORS(ITestOutputHelper output)
    {
      _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    [SmartTheory(Execute.Always, On.All)]
    [InlineData("Development", false, "shared-logging-config.json", "cors-config-01.json")]
    [InlineData("Production", true, "shared-logging-config.json", "cors-config-01.json")]
    [UnitTest]

    public void GivenOptionsWithAllowAny_WhenInitializing_ThenValidationShouldBeEnvironmentDependent(string environment, bool shouldFail, params string[] files)
    {
      using var scope = EnvironmentVariableScope.Create(Constants.EnvironmentVariables.DotNet.Environment, environment);
      var config = new ConfigurationBuilder()
        .UseEmbeddedConfiguration(typeof(CORS).Assembly, "Hive.MicroServices.Tests", files)
        .Build();

      var service = (MicroService)new MicroService(ServiceName)
        .InTestClass<MicroServiceTests>()
        .WithLogging(log => log.ToXunit(_output))
        .WithCors()
        .ConfigureDefaultServicePipeline();

      // Act
      var action = async () => { await service.InitializeAsync(config); };

      // Assert
      if (shouldFail)
      {
        action.Should().Throw<OptionsValidationException>().And.Message.Should().Be(OptionsValidator.Errors.AllowAnyNotAllowed);
      }
      else
      {
        action.Should().NotThrow();
      }
    }

    [SmartTheory(Execute.Always, On.All)]
    [InlineData(true, "shared-logging-config.json", "cors-config-02.json")]
    [InlineData(true, "shared-logging-config.json", "cors-config-03.json")]
    [InlineData(false, "shared-logging-config.json", "cors-config-04.json")]
    [UnitTest]

    public void GivenOptionsWithoutAllowAny_WhenInitializing_ThenAtLeastOnePolicyMustBeDefined(bool shouldFail, params string[] files)
    {
      using var scope = EnvironmentVariableScope.Create(Constants.EnvironmentVariables.DotNet.Environment, "Development");
      var config = new ConfigurationBuilder()
        .UseEmbeddedConfiguration(typeof(CORS).Assembly, "Hive.MicroServices.Tests", files)
        .Build();

      var service = (MicroService)new MicroService(ServiceName)
        .InTestClass<MicroServiceTests>()
        .WithLogging(log => log.ToXunit(_output))
        .WithCors()
        .ConfigureDefaultServicePipeline();

      // Act
      var action = async () => { await service.InitializeAsync(config); };

      // Assert
      if (shouldFail)
      {
        action.Should().Throw<OptionsValidationException>().And.Message.Should().Be(OptionsValidator.Errors.NoPolicies);
      }
      else
      {
        action.Should().NotThrow();
      }
    }

    [SmartTheory(Execute.Always, On.All)]
    [InlineData(true, CORSPolicyValidator.Errors.NameRequired,"shared-logging-config.json", "cors-config-05.json")]
    [InlineData(false, null,"shared-logging-config.json", "cors-config-06.json")]
    [InlineData(false, null,"shared-logging-config.json", "cors-config-07.json")]
    [InlineData(false, null,"shared-logging-config.json", "cors-config-08.json")]
    [InlineData(true, CORSPolicyValidator.Errors.PolicyEmpty,"shared-logging-config.json", "cors-config-09.json")]
    [InlineData(true, CORSPolicyValidator.Errors.AllowedOriginsInvalidFormat,"shared-logging-config.json", "cors-config-10.json")]
    [InlineData(true, CORSPolicyValidator.Errors.AllowedMethodsInvalidValue,"shared-logging-config.json", "cors-config-11.json")]
    [UnitTest]
    public void GivenOptionsWithPolicies_WhenInitializing_ThenPoliciesAreValidated(bool shouldFail, string expectedError, params string[] files)
    {
      using var scope = EnvironmentVariableScope.Create(Constants.EnvironmentVariables.DotNet.Environment, "Development");
      var config = new ConfigurationBuilder()
        .UseEmbeddedConfiguration(typeof(CORS).Assembly, "Hive.MicroServices.Tests", files)
        .Build();

      var service = (MicroService)new MicroService(ServiceName)
        .InTestClass<MicroServiceTests>()
        .WithLogging(log => log.ToXunit(_output))
        .WithCors()
        .ConfigureDefaultServicePipeline();

      // Act
      var action = async () => { await service.InitializeAsync(config); };

      // Assert
      if (shouldFail)
      {
        action.Should().Throw<OptionsValidationException>().And.Message.Should().Be(expectedError);
      }
      else
      {
        action.Should().NotThrow();
      }
    }
  }
}

