using FluentAssertions;
using Hive.Configuration;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hive.Abstractions.Tests.Configuration;

public class ConfigureValidatedOptionsTests : ConfigurationFixture
{
  [SmartTheory(Execute.Always, On.All)]
  [InlineData("simple-options-01.json", true, null, null)]
  [InlineData("simple-options-02.json", false, "Name", "required")]
  [InlineData("simple-options-03.json", false, "Name", "minimum")]
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