using CloudTek.Testing;
using FluentAssertions;
using FluentValidation;
using Hive.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hive.Abstractions.Tests.Configuration;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class PostConfigurationTests
{
  public class FluentValidation
  {
    [SmartTheory(Execute.Always, On.All)]
    [InlineData("simple-options-01.json", true, null, null)]
    [InlineData("simple-options-02.json", false, "Name", "empty")]
    [InlineData("simple-options-03.json", false, "Name", "at least")]
    [UnitTest]
    public void
      GivenSimpleOptionsSectionExists_WhenConfigureValidatedOptions_ThenOptionsAreAvailableWhenResolvingFromContainerAndPropertiesAreBound(
        string config, bool shouldBeValid, string? key, string? error)
    {
      var cfg = GetConfigurationRoot(config);

      var provider = new ServiceCollection()
        .AddSingleton<IConfiguration>(cfg)
        .AddScoped<IValidator<SimpleOptions>, SimpleOptionsFluentValidator>()
        .ConfigureValidatedOptions<SimpleOptions, SimpleOptionsFluentValidator>(cfg, () => SimpleOptions.SectionKey)
        .BuildServiceProvider();

      // Act & Assert
      var action = () =>
      {
        var options = provider.GetRequiredService<IOptions<SimpleOptions>>().Value;
        options.Should().NotBeNull();
      };

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
}