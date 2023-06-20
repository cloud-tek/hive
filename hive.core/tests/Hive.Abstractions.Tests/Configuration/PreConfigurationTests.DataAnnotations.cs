using FluentAssertions;
using Hive.Configuration;
using Hive.Exceptions;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hive.Tests.Configuration;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class PreConfigurationTests
{
  public class DataAnnotations
  {
    [SmartTheory(Execute.Always, On.All)]
    [InlineData("simple-options-01.json", true, null, null)]
    [InlineData("simple-options-02.json", false, "Name", "required")]
    [InlineData("simple-options-03.json", false, "Name", "minimum")]
    [UnitTest]
    public void
      GivenSimpleOptionsSectionExists_WhenPreConfigureValidatedOptions_ThenOptionsAreImmediatelyAvailableAndPropertiesAreBound(
        string config, bool shouldBeValid, string? key, string? error)
    {
      var cfg = GetConfigurationRoot(config);

      // Act & Assert
      var action = () =>
      {
        var options = new ServiceCollection()
          .PreConfigureValidatedOptions<SimpleOptions>(cfg,  () => SimpleOptions.SectionKey);
      };

      if (shouldBeValid)
      {
        action.Should().NotThrow();
      }
      else
      {
        action.Should().Throw<ConfigurationException>().And.Message.Should().ContainAll(new[] { key, error });
      }
    }
  }
}
