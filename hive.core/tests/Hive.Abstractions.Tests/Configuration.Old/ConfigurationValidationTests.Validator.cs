using FluentAssertions;
using Hive.Configuration;
using Hive.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using Hive.Exceptions;
using Xunit;

namespace Hive.Tests.Configuration;

public partial class ConfigurationValidationTests
{


    // [SmartTheory(Execute.Always, On.All)]
    // [InlineData("test-validator-options01.json", true, null, null)]
    public void
      GivenGivenSectionExists_WhenPreConfigureValidatedOptionsIsInvokedWithCustomValidator_ThenOptionsAreValidated(
        string config, bool shouldBeValid, string? key, string? error)
    {
      // Arrange
      var cfg = GetConfigurationRoot(config);

      // Act
      var action = new Func<IOptions<Options>>(() =>
      {
        var options = new ServiceCollection()
          .PreConfigureValidatedOptions<Options, OptionsValidator>(cfg, () => Options.SectionKey);

        return options;
      });

      // Assert

      if (shouldBeValid)
      {
        action().Should().NotBeNull();
      }
      else
      {
        action.Should().Throw<ConfigurationException>();
      }
    }


    // [SmartTheory(Execute.Always, On.All)]
    // [InlineData("test-validator-options01.json", true, null, null)]
    // [InlineData("test-validator-options02.json", false, "Children", "minimum length")]
    // [InlineData("test-validator-options03.json", false, "Children", "minimum length")]
    // [InlineData("test-validator-options04.json", false, "Name", "required")]
    // [UnitTest]
    public void
      GivenSectionExists_WhenConfigureValidatedOptionsIsInvokedWithCustomValidator_ThenOptionsAreValidatedWhenResolvingFromContainer(
        string config, bool shouldBeValid, string? key, string? error)
    {
      // Arrange
      var cfg = GetConfigurationRoot(config);

      var provider = new ServiceCollection()
        .ConfigureValidatedOptions<Options, OptionsValidator>(cfg, () => Options.SectionKey)
        .BuildServiceProvider();

      // Act
      var action = () =>
      {
        var options = provider.GetRequiredService<IOptions<Options>>().Value;
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

    // ReSharper disable once ClassNeverInstantiated.Global
}
