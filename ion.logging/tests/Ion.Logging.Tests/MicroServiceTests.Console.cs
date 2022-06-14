using FluentAssertions;
using Ion.MicroServices;
using Ion.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ion.Logging.Tests
{
    public partial class MicroServiceTests
    {
        private const string ServiceName = "test-service";
        public class Console
        {
            [Fact]
            [UnitTest]
            public async void
                GivenWithLoggingToConsole_WhenValidConfiguration_ThenServiceStarts()
            {
                // Arrange
                var config = new ConfigurationBuilder()
                    .UseDefaultLoggingConfiguration()
                    .Build();
                    

                var service = new MicroService(ServiceName)
                    .InTestClass<MicroServiceTests>()
                    .WithLogging(log =>
                    {
                        log.ToConsole();
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
}