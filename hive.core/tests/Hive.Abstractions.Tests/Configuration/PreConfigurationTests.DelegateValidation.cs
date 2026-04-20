using CloudTek.Testing;
using FluentAssertions;
using Hive.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hive.Abstractions.Tests.Configuration;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class PreConfigurationTests
{
  public class DelegateValidation
  {
    [SmartTheory(Execute.Always, On.All)]
    [InlineData("simple-options-01.json", true, null)]
    [InlineData("simple-options-02.json", false, "Name")]
    [InlineData("simple-options-03.json", false, "Name")]
    [UnitTest]
    public void
      GivenSimpleOptionsSectionExists_WhenPreConfigureValidatedOptions_ThenOptionsAreImmediatelyAvailableAndPropertiesAreBound(
        string config, bool shouldBeValid, string? key)
    {
      var cfg = GetConfigurationRoot(config);

      // Act & Assert
      var action = () =>
      {
        var options = new ServiceCollection()
          .PreConfigureValidatedOptions<SimpleOptions>(cfg, () => SimpleOptions.SectionKey, (o) => !string.IsNullOrEmpty(o.Name) && o.Name.Length >= 3);

        options.Value.Should().NotBeNull();
        options.Value.Name.Should().NotBeNullOrEmpty();
      };

      if (shouldBeValid)
      {
        action.Should().NotThrow();
      }
      else if (key != null)
      {
        action.Should().Throw<OptionsValidationException>().And.Message.Should().Be("Options validation failed");
      }
      else
      {
        throw new NotSupportedException("Unhandled test case");
      }
    }
  }
}