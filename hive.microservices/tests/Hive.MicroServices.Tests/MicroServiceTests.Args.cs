using System.Threading.Tasks;
using FluentAssertions;
using Hive.MicroServices.Extensions;
using CloudTek.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hive.MicroServices.Tests;

public partial class MicroServiceTests
{
  public class Args
  {
    private const string ServiceName = "microservice-tests-startup";

    [SmartFact(Execute.Always, On.All)]
    [UnitTest]

    public async Task GivenMicroServiceIsStarting_WhenInitalizeAsyncWithArgs_ThenArgsAvailableInIMicroService()
    {
      // Arrange
      var args = new[] { "--arg1", "value1", "--arg2", "value2" };
      var config = new ConfigurationBuilder().Build();

      var service = (MicroService)new MicroService(ServiceName, new NullLogger<IMicroService>())
          .InTestClass<MicroServiceTests>()
          .ConfigureDefaultServicePipeline();

      // Act
      await service.InitializeAsync(config, args);

      // Assert
      service.Args.Should().BeEquivalentTo(args);
    }
  }
}