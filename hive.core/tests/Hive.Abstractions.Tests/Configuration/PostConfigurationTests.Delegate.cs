using System;
using FluentAssertions;
using Hive.Configuration;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hive.Tests.Configuration;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class PostConfigurationTests
{
  public class Delegate
  {
    [SmartTheory(Execute.Always, On.All)]
    [InlineData("simple-options-01.json", true, null)]
    [InlineData("simple-options-02.json", false, "Name")]
    [InlineData("simple-options-03.json", false, "Name")]
    [UnitTest]
    public void
      GivenOptions1SectionExists_WhenConfigureValidatedOptions_ThenOptionsAreAvailableWhenResolvingFromContainerAndPropertiesAreBound(
        string config, bool shouldBeValid, string? key)
    {
      var cfg = GetConfigurationRoot(config);

      var provider = new ServiceCollection()
        .AddSingleton<IConfiguration>(cfg)
        .ConfigureValidatedOptions<SimpleOptions>(cfg, () => SimpleOptions.SectionKey, (o) => !string.IsNullOrEmpty(o.Name) && o.Name.Length >= 3)
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
      else if(key != null)
      {
        action.Should().Throw<OptionsValidationException>().And.Message.Should().Be("A validation error has occurred.");
      }
      else
      {
        throw new NotImplementedException("Unhandled test case");
      }
    }
  }
}
