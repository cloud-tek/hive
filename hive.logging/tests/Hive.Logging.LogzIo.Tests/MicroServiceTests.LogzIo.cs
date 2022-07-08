using FluentAssertions;
using Hive.MicroServices;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Hive.Logging.LogzIo.Tests;

public class MicroServiceTests
{
    private const string ServiceName = "test-service";
    
    public class LogzIo
    {
        [SmartFact(Execute.Always, On.All, "Ion__Logging__LogzIo__Region", "Ion__Logging__LogzIo__Token")]
        [UnitTest]
        public async void
            GivenWithLoggingToLogzIo_WhenValidConfiguration_ThenServiceStarts()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .UseDefaultLoggingConfiguration()
                .UseTestLogzIoConfiguration()
                .Build();

            var service = new MicroService(ServiceName)
                .InTestClass<MicroServiceTests>()
                .WithLogging(log =>
                {
                    log.ToLogzIo();
                })
                .ConfigureDefaultServicePipeline();

            service.CancellationTokenSource.CancelAfter(1000);

            // Act
            var action = async () => await service.RunAsync(config);

            // Assert
            await action.Should().NotThrowAsync();
        }
    }
}