using System;
using System.Collections.Generic;
using FluentAssertions;
using Hive.Configuration;
using Hive.Exceptions;
using Hive.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hive.Abstractions.Tests.Configuration;

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
          .PreConfigureValidatedOptions<SimpleOptions, SimpleOptionsFluentValidator>(cfg, () => SimpleOptions.SectionKey);
      };

      if (shouldBeValid)
      {
        action.Should().NotThrow();
      }
      else if (key != null)
      {
        var tokens = new List<string>();
        tokens.Add(key);
        tokens.AddRange(errors);
        action.Should().Throw<OptionsValidationException>().And.Message.Should().ContainAll(tokens.ToArray());
      }
      else
      {
        throw new NotSupportedException("Unhandled test case");
      }
    }
  }
}