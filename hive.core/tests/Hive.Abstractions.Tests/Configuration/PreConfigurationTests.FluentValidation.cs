using System.Collections.Generic;
using FluentAssertions;
using Hive.Configuration;
using Hive.Exceptions;
using Hive.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hive.Tests.Configuration;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class PreConfigurationTests
{
  public class FluentValidation
  {
    [SmartTheory(Execute.Always, On.All)]
    [InlineData("simple-options-01.json", true, null, null)]
    [InlineData("simple-options-02.json", false, "Name", "empty", "at least")]
    [InlineData("simple-options-03.json", false, "Name", "at least")]
    [UnitTest]
    public void
      GivenSimpleOptionsSectionExists_WhenPreConfigureFluentlyValidatedOptions_ThenOptionsAreImmediatelyAvailableAndPropertiesAreBound(
        string config, bool shouldBeValid, string? key, params string[] errors)
    {
      var cfg = GetConfigurationRoot(config);

      // Act & Assert
      var action = () =>
      {
        var options = new ServiceCollection()
          .PreConfigureFluentlyValidateOptions<SimpleOptions, SimpleOptionsFluentValiator>(cfg,  () => SimpleOptions.SectionKey);
      };

      if (shouldBeValid)
      {
        action.Should().NotThrow();
      }
      else
      {
        var tokens = new List<string>();
        tokens.Add(key);
        tokens.AddRange(errors);
        action.Should().Throw<ConfigurationException>().And.Message.Should().ContainAll(tokens.ToArray());
      }
    }
  }
}
