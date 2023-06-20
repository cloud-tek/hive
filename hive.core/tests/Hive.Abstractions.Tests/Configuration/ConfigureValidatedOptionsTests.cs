using FluentAssertions;
using Hive.Configuration;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hive.Tests.Configuration;

public class ConfigureValidatedOptionsTests : ConfigurationFixture
{
  // [SmartTheory(Execute.Always, On.All)]
  // [InlineData("options1-01.json", true, null, null)]
  // [InlineData("options1-02.json", false, "Name", "min")]
  // [InlineData("options1-03.json", false, "Name", "required")]
  // [InlineData("test-validator-options02.json", false, "Children", "minimum length")]
  // [InlineData("test-validator-options03.json", false, "Children", "minimum length")]

  //[UnitTest]
  public void
    GivenSectionExists_WhenConfigureValidatedOptions_ThenOptionsAreValidatedWhenResolvingFromContainerUsingDataAnnotations(
      string config, bool shouldBeValid, string? key, string? error)
  {
    // Arrange
    var cfg = GetConfigurationRoot(config);

    var provider = new ServiceCollection()
      .AddSingleton<IConfigurationRoot>(cfg)
      .AddSingleton<IConfiguration>(cfg)
      .ConfigureValidatedOptions<SimpleOptions>(cfg, () => SimpleOptions.SectionKey)
      //.ConfigureValidatedOptions<ConfigurationValidationTests.Options, ConfigurationValidationTests.OptionsValidator>(cfg, () => ConfigurationValidationTests.Options.SectionKey)
      .BuildServiceProvider();

    // Act
    var action = () =>
    {
      var options = provider.GetRequiredService<IOptions<SimpleOptions>>().Value;
      options.GetType();
    };

    // Assert
    if (shouldBeValid)
    {
      action.Should().NotThrow();
    }
    else
    {
      action.Should().Throw<OptionsValidationException>().And.Message.Should().ContainAll(new[] { key, error });
    }
  }
}
