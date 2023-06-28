using System.Collections.Generic;
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
        action.Should().Throw<OptionsValidationException>().And.Message.Should().ContainAll(new[] { key, error });
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
      GivenComplexOptionsSectionExists_WhenPreConfigureValidatedOptions_ThenOptionsAreImmediatelyAvailableAndPropertiesAreBound(
        string config, bool shouldBeValid, string? key, params string[] errors)
    {
      var cfg = GetConfigurationRoot(config);

      // Act & Assert
      var action = () =>
      {
        var options = new ServiceCollection()
          .PreConfigureValidatedOptions<ComplexOptions>(cfg,  () => ComplexOptions.SectionKey);
      };

      if (shouldBeValid)
      {
        action.Should().NotThrow();
      }
      else
      {
        var tokens = new List<string>();
        tokens.AddRange(errors);
        var ex = action.Should().Throw<OptionsValidationException>();
        ex.And.Message.Should().ContainAll(tokens.ToArray());
      }
    }
  }
}
