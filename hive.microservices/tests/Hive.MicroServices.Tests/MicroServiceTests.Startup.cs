using System.Threading.Tasks;
using FluentAssertions;
using Hive.Extensions;
using Hive.MicroServices.Api;
using Hive.MicroServices.Extensions;
using Hive.MicroServices.GraphQL;
using Hive.MicroServices.Grpc;
using Hive.MicroServices.Lifecycle;
using Hive.MicroServices.Testing;
using CloudTek.Testing;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hive.MicroServices.Tests;

public partial class MicroServiceTests
{
  public class Startup
  {
    private const string ServiceName = "microservice-tests-startup";

    [Fact]
    [UnitTest]
    public async Task GivenConfigureDefaultServicePipelineIsUsed_WhenRunAsyncIsInvoked_ThenServiceStartsInNoneMode()
    {
      // Arrange
      var config = new ConfigurationBuilder().Build();

      var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .ConfigureDefaultServicePipeline();

      service.CancellationTokenSource.CancelAfter(1000);

      // Act
      await service.RunAsync(config);

      // Assert
      service.PipelineMode.Should().Be(MicroServicePipelineMode.None);
    }

    [Fact]
    [UnitTest]
    public async Task GivenRunAsyncIsInvoked_WhenNoIHostedStartupServicesAreUsed_ThenServiceShouldStartImmediately()
    {
      // Arrange
      var config = new ConfigurationBuilder().Build();

      await using var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .ConfigureDefaultServicePipeline()
        .ConfigureTestHost();

      service.CancellationTokenSource.CancelAfter(1000);

      // Act
      await service.InitializeAsync(config);
      var startTask = service.StartAsync();

      // Assert
      service.ShouldStart(500.Milliseconds());

      await startTask;
      await service.StopAsync();
    }

    [Fact]
    [UnitTest]
    public async Task GivenRunAsyncIsInvoked_WhenNonFailingIHostedStartupServicesAreUsed_ThenServiceShouldStart()
    {
      // Arrange
      var config = new ConfigurationBuilder().Build();

      await using var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .ConfigureServices(
          (services, configuration) =>
          {
            services.AddHostedStartupService<TestData.Sec2DelayStartupService>();
          })
        .ConfigureDefaultServicePipeline()
        .ConfigureTestHost();

      service.CancellationTokenSource.CancelAfter(5000);

      // Act
      await service.InitializeAsync(config);
      var startTask = service.StartAsync();

      // Assert
      service.ShouldStart(5000.Milliseconds());

      await startTask;
      await service.StopAsync();
    }

    [Fact]
    [UnitTest]
    public async Task GivenRunAsyncIsInvoked_WhenFailingIHostedStartupServicesAreUsed_ThenServiceShouldFailToStart()
    {
      // Arrange
      var config = new ConfigurationBuilder().Build();

      await using var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .ConfigureServices(
          (services, configuration) =>
          {
            services.AddHostedStartupService<TestData.FailingSec2DelayStartupService>();
          })
        .ConfigureDefaultServicePipeline()
        .ConfigureTestHost();

      // Act
      await service.InitializeAsync(config);
      var startTask = service.StartAsync();

      // Assert
      service.ShouldFailToStart(5000.Milliseconds());

      await startTask;
    }

    [Fact]
    [UnitTest]
    public async Task GivenConfigureApiPipelineIsUsed_WhenRunAsyncIsInvoked_ThenServiceStartsInApiMode()
    {
      // Arrange
      var config = new ConfigurationBuilder().Build();

      var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .ConfigureApiPipeline(
          (x) =>
          {
          });

      service.CancellationTokenSource.CancelAfter(1000);

      // Act
      await service.RunAsync(config);

      // Assert
      service.PipelineMode.Should().Be(MicroServicePipelineMode.Api);
    }

    [Fact]
    [UnitTest]
    public async Task
      GivenConfigureApiControllerPipelineIsUsed_WhenRunAsyncIsInvoked_ThenServiceStartsInApiControllersMode()
    {
      // Arrange
      var config = new ConfigurationBuilder().Build();

      var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .ConfigureApiControllerPipeline();

      service.CancellationTokenSource.CancelAfter(1000);

      // Act
      await service.RunAsync(config);

      // Assert
      service.PipelineMode.Should().Be(MicroServicePipelineMode.ApiControllers);
    }

    [Fact]
    [UnitTest]
    public async Task GivenConfigureGraphQLPipelineIsUsed_WhenRunAsyncIsInvoked_ThenServiceStartsInGraphQLMode()
    {
      // Arrange
      var config = new ConfigurationBuilder().Build();

      var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .ConfigureGraphQLPipeline(
          (x) =>
          {
          });

      service.CancellationTokenSource.CancelAfter(1000);

      // Act
      await service.RunAsync(config);

      // Assert
      service.PipelineMode.Should().Be(MicroServicePipelineMode.GraphQL);
    }

    [Fact]
    [UnitTest]
    public async Task GivenConfigureGrpcPipelineIsUsed_WhenRunAsyncIsInvoked_ThenServiceStartsInGrpcMode()
    {
      // Arrange
      var config = new ConfigurationBuilder().Build();

      var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .ConfigureGrpcPipeline(
          (x) =>
          {
          });

      service.CancellationTokenSource.CancelAfter(1000);

      // Act
      await service.RunAsync(config);

      // Assert
      service.PipelineMode.Should().Be(MicroServicePipelineMode.Grpc);
    }

    [Fact]
    [UnitTest]
    public async Task GivenConfigureCodeFirstGrpcPipelineIsUsed_WhenRunAsyncIsInvoked_ThenServiceStartsInGrpcMode()
    {
      // Arrange
      var config = new ConfigurationBuilder().Build();

      var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
        .InTestClass<MicroServiceTests>()
        .ConfigureCodeFirstGrpcPipeline(
          (x) =>
          {
          });

      service.CancellationTokenSource.CancelAfter(1000);

      // Act
      await service.RunAsync(config);

      // Assert
      service.PipelineMode.Should().Be(MicroServicePipelineMode.Grpc);
    }
  }
}