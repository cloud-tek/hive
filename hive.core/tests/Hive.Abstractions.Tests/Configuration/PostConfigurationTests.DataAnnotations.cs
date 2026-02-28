using FluentAssertions;
using Hive.Configuration;
using CloudTek.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hive.Abstractions.Tests.Configuration;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class PostConfigurationTests
{
  public class DataAnnotations
  {
    [SmartTheory(Execute.Always, On.All)]
    [InlineData("simple-options-01.json", true, null, null)]
    [InlineData("simple-options-02.json", false, "Name", "required")]
    [InlineData("simple-options-03.json", false, "Name", "minimum")]
    [UnitTest]
    public void
      GivenSimpleOptionsSectionExists_WhenConfigureValidatedOptions_ThenOptionsAreAvailableWhenResolvingFromContainerAndPropertiesAreBound(
        string config, bool shouldBeValid, string? key, params string[]? errors)
    {
      var cfg = GetConfigurationRoot(config);

      var provider = new ServiceCollection()
        .AddSingleton<IConfiguration>(cfg)
        .ConfigureValidatedOptions<SimpleOptions>(cfg, () => SimpleOptions.SectionKey)
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
        var tokens = new List<string>();
        tokens.AddRange(errors ?? []);
        var ex = action.Should().Throw<OptionsValidationException>();
        ex.And.Message.Should().Contain(key);
        ex.And.Message.Should().ContainAll(tokens.ToArray());
      }
    }

    [SmartTheory(Execute.Always, On.All)]
    [InlineData("complex-options-01.json", true, null, null)]
    [InlineData("complex-options-02.json", false, "Children", "minimum")]
    [InlineData("complex-options-03.json", false, "Children", "minimum")]
    [InlineData("complex-options-04.json", false, "Children[0].Name", "required")]
    [InlineData("complex-options-05.json", false, "Children[1].Name", "minimum")]
    [UnitTest]
    public void
      GivenComplexOptionsSectionExists_WhenConfigureValidatedOptions_ThenOptionsAreAvailableWhenResolvingFromContainerAndPropertiesAreBound(
        string config, bool shouldBeValid, string? key, string? error)
    {
      var cfg = GetConfigurationRoot(config);

      var provider = new ServiceCollection()
        .AddSingleton<IConfiguration>(cfg)
        .ConfigureValidatedOptions<ComplexOptions>(cfg, () => ComplexOptions.SectionKey)
        .BuildServiceProvider();

      // Act & Assert
      var action = () =>
      {
        var options = provider.GetRequiredService<IOptions<ComplexOptions>>().Value;
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