using FluentAssertions;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Hive.MicroServices.Tests;

public partial class MicroServiceTests
{
    public class Services
    {
        private const string ServiceName = "microservice-tests-startup";

        [SmartTheory(Execute.Always, On.All)]
        [InlineData(typeof(IConfigurationRoot))]
        [InlineData(typeof(IConfiguration))]
        [InlineData(typeof(IMicroService))]
        [UnitTest]

        public async Task GivenMicroServiceIsStarting_WhenInitalizeAsync_ThenRequiredTypesAreResolveable(Type type)
        {
            // Arrange
            var config = new ConfigurationBuilder().Build();

            var service = (MicroService)new MicroService(ServiceName, new NullLogger<IMicroService>())
                .InTestClass<MicroServiceTests>()
                .ConfigureDefaultServicePipeline();

            // Act
            await service.InitializeAsync(config);

            // Assert
            service.ServiceProvider.GetService(type).Should().NotBeNull();
        }
    }
}