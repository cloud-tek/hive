using System.Threading.Tasks;
using FluentAssertions;
using Ion.Extensions;
using Ion.MicroServices.Lifecycle;
using Ion.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ion.MicroServices.Tests;

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

            var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
                .InTestClass<MicroServiceTests>()
                .ConfigureDefaultServicePipeline();

            service.CancellationTokenSource.CancelAfter(1000);

            // Act & Assert
            var task = service.RunAsync(config);

            service.ShouldStart(500.Milliseconds());

            await task;
        }

        [Fact]
        [UnitTest]
        public async Task GivenRunAsyncIsInvoked_WhenNonFailingIHostedStartupServicesAreUsed_ThenServiceShouldStart()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();

            var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
                .InTestClass<MicroServiceTests>()
                .ConfigureServices((services, configuration) =>
                {
                    services.AddHostedStartupService<TestData.Sec2DelayStartupService>();
                })
                .ConfigureDefaultServicePipeline();

            service.CancellationTokenSource.CancelAfter(1000);

            // Act & Assert
            var task = service.RunAsync(config);

            service.ShouldStart(5000.Milliseconds());

            await task;
        }

        [Fact]
        [UnitTest]
        public async Task GivenRunAsyncIsInvoked_WhenFailingIHostedStartupServicesAreUsed_ThenServiceShouldFailToStart()
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();

            var service = new MicroService(ServiceName, new NullLogger<IMicroService>())
                .InTestClass<MicroServiceTests>()
                .ConfigureServices((services, configuration) =>
                {
                    services.AddHostedStartupService<TestData.FailingSec2DelayStartupService>();
                })
                .ConfigureDefaultServicePipeline();

            // Act & Assert
            var task = service.RunAsync(config);

            service.ShouldFailToStart(5000.Milliseconds());

            await task;
        }
    }
}