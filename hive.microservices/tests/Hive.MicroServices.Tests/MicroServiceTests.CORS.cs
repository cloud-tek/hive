using FluentAssertions;
using Hive.Configuration.CORS;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
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

    // TODO: Revisit to support proper xunit logging via _output instead of NullLogger
    // The tests currently use NullLogger to avoid NullReferenceException in CORS Extension.cs:67
    // when validation fails. Need to implement a logger factory or extension method that properly
    // wires up xunit logging to MicroService instances in test scenarios.

    [SmartTheory(Execute.Always, On.All)]
    [InlineData("Development", false, "shared-logging-config.json", "cors-config-01.json")]
    [InlineData("Production", true, "shared-logging-config.json", "cors-config-01.json")]
    [UnitTest]

    public async Task GivenOptionsWithAllowAny_WhenInitializing_ThenValidationShouldBeEnvironmentDependent(string environment, bool shouldFail, params string[] files)
    {
      using var scope = EnvironmentVariableScope.Create(Constants.EnvironmentVariables.DotNet.Environment, environment);
      var config = new ConfigurationBuilder()
        .UseEmbeddedConfiguration(typeof(CORS).Assembly, "Hive.MicroServices.Tests", files)
        .Build();

      var service = (MicroService)new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .WithCORS()
        .ConfigureDefaultServicePipeline();

      // Act
      var action = async () => { await service.InitializeAsync(config); };

      // Assert
      if (shouldFail)
      {
        (await action.Should().ThrowAsync<OptionsValidationException>()).And.Message.Should().Be(OptionsValidator.Errors.AllowAnyNotAllowed);
      }
      else
      {
        await action.Should().NotThrowAsync();
      }
    }

    [SmartTheory(Execute.Always, On.All)]
    [InlineData(true, "shared-logging-config.json", "cors-config-02.json")]
    [InlineData(true, "shared-logging-config.json", "cors-config-03.json")]
    [InlineData(false, "shared-logging-config.json", "cors-config-04.json")]
    [UnitTest]

    public async Task GivenOptionsWithoutAllowAny_WhenInitializing_ThenAtLeastOnePolicyMustBeDefined(bool shouldFail, params string[] files)
    {
      using var scope = EnvironmentVariableScope.Create(Constants.EnvironmentVariables.DotNet.Environment, "Development");
      var config = new ConfigurationBuilder()
        .UseEmbeddedConfiguration(typeof(CORS).Assembly, "Hive.MicroServices.Tests", files)
        .Build();

      var service = (MicroService)new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .WithCORS()
        .ConfigureDefaultServicePipeline();

      // Act
      var action = async () => { await service.InitializeAsync(config); };

      // Assert
      if (shouldFail)
      {
        (await action.Should().ThrowAsync<OptionsValidationException>()).And.Message.Should().Be(OptionsValidator.Errors.NoPolicies);
      }
      else
      {
        await action.Should().NotThrowAsync();
      }
    }

    [SmartTheory(Execute.Always, On.All)]
    [InlineData(true, CORSPolicyValidator.Errors.NameRequired, "shared-logging-config.json", "cors-config-05.json")]
    [InlineData(false, null, "shared-logging-config.json", "cors-config-06.json")]
    [InlineData(false, null, "shared-logging-config.json", "cors-config-07.json")]
    [InlineData(false, null, "shared-logging-config.json", "cors-config-08.json")]
    [InlineData(true, CORSPolicyValidator.Errors.PolicyEmpty, "shared-logging-config.json", "cors-config-09.json")]
    [InlineData(true, CORSPolicyValidator.Errors.AllowedOriginsInvalidFormat, "shared-logging-config.json", "cors-config-10.json")]
    [InlineData(true, CORSPolicyValidator.Errors.AllowedMethodsInvalidValue, "shared-logging-config.json", "cors-config-11.json")]
    [UnitTest]
    public async Task GivenOptionsWithPolicies_WhenInitializing_ThenPoliciesAreValidated(bool shouldFail, string? expectedError, params string[] files)
    {
      using var scope = EnvironmentVariableScope.Create(Constants.EnvironmentVariables.DotNet.Environment, "Development");
      var config = new ConfigurationBuilder()
        .UseEmbeddedConfiguration(typeof(CORS).Assembly, "Hive.MicroServices.Tests", files)
        .Build();

      var service = (MicroService)new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .WithCORS()
        .ConfigureDefaultServicePipeline();

      // Act
      var action = async () => { await service.InitializeAsync(config); };

      // Assert
      if (shouldFail)
      {
        (await action.Should().ThrowAsync<OptionsValidationException>()).And.Message.Should().Be(expectedError);
      }
      else
      {
        await action.Should().NotThrowAsync();
      }
    }
  }
}