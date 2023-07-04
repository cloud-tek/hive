using FluentAssertions;
using Hive.Configuration;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hive.Tests.Configuration;

public partial class PostConfigurationTests : ConfigurationFixture
{
  [SmartTheory(Execute.Always, On.All)]
  [InlineData("simple-options-01.json", "Test")]
  [UnitTest]
  public void
    GivenSectionExists_WhenConfigureOptions_ThenOptionsAreAvailableWhenResolvingFromContainerAndPropertiesAreBound(
      string config, string expectedName)
  {
    // Arrange
    var cfg = GetConfigurationRoot(config);

    var provider = new ServiceCollection()
      .AddSingleton<IConfiguration>(cfg)
      .ConfigureOptions<SimpleOptions>(cfg, () => SimpleOptions.SectionKey)
      .BuildServiceProvider();

    // Act & Assert
    var action = () =>
    {
      var options = provider.GetRequiredService<IOptions<SimpleOptions>>().Value;

      options.Name.Should().Be(expectedName, because: "Name property should be correctly bound");
    };

    action.Should().NotThrow();
  }
}
