using FluentAssertions;
using Hive.Configuration;
using Hive.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Hive.Tests.Configuration;

public partial class ConfigurationValidationTests
{
    public class DataAnnotated
    {
        public class Options
        {
            public const string SectionKey = "TestOptions";

            [Required] public string? Setting1 { get; set; }
            [Required] public string? Setting2 { get; set; }
            [Required, Url] public string? Setting3 { get; set; }
            [Required, EmailAddress] public string? Setting4 { get; set; }
        }

        // [SmartTheory(Execute.Always, On.All)]
        // [InlineData("test-dataannotations-options01.json", true, null, null)]
        // [InlineData("test-dataannotations-options02.json", false, "Setting2", "required")]
        // [InlineData("test-dataannotations-options03.json", false, "Setting3", "URL")]
        // [InlineData("test-dataannotations-options04.json", false, "Setting4", "e-mail")]
        // [UnitTest]
        public void
            GivenSectionExists_WhenConfigureValidatedOptionsIsInvoked_ThenOptionsAreValidatedWhenResolvingFromContainer(
                string config, bool shouldBeValid, string? key, string? error)
        {
            // Arrange
            var cfg = GetConfigurationRoot(config);

            var provider = new ServiceCollection()
                .ConfigureValidatedOptions<Options>(cfg, () => Options.SectionKey)
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
    }
}
