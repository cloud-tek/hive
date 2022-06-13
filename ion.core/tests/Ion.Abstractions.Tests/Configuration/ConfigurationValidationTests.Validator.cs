using FluentAssertions;
using Ion.Configuration;
using Ion.Configuration.Validation;
using Ion.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Xunit;

namespace Ion.Tests.Configuration;

public partial class ConfigurationValidationTests
{
    public class Validator
    {
        [SmartTheory(Execute.Always, On.All)]
        [InlineData("test-validator-options01.json", true, null, null)]
        [InlineData("test-validator-options02.json", false, "Children", "minimum length")]
        [InlineData("test-validator-options03.json", false, "Children", "minimum length")]
        [InlineData("test-validator-options04.json", false, "Name", "required")]
        [UnitTest]
        public void GivenSectionExists_WhenConfigureValidatedOptionsIsInvokedWithCustomValidator_ThenOptionsAreValidatedWhenResolvingFromContaine(string config, bool shouldBeValid, string? key, string? error)
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

        public class ChildOptions
        {
            [Required]
            public string? Name { get; set; }
        }

        public class Options
        {
            public const string SectionKey = "TestOptions";

            [ValidateCollection, Required, MinLength(1)]
            public ChildOptions[] Children { get; set; } = Array.Empty<ChildOptions>();

            [Required]
            public string? Name { get; set; }
        }

        public class OptionsValidator : IValidateOptions<Options>
        {
            public ValidateOptionsResult Validate(string name, Options options)
            {
                var validationResults = global::Ion.Configuration.Validation.Validator.ValidateReturnValue(options);
                if (validationResults.Any())
                {
                    var builder = new StringBuilder();
                    foreach (var result in validationResults)
                    {
                        var pretty = PrettyPrint(result, string.Empty, true);
                        builder.Append(pretty);
                    }
                    return ValidateOptionsResult.Fail(builder.ToString());
                }

                return ValidateOptionsResult.Success;
            }

            private string PrettyPrint(global::Ion.Configuration.Validation.ValidationResult root, string indent, bool last)
            {
                // Based on https://stackoverflow.com/a/1649223
                var sb = new StringBuilder();
                sb.Append(indent);
                if (last)
                {
                    sb.Append("|-");
                    indent += "  ";
                }
                else
                {
                    sb.Append("|-");
                    indent += "| ";
                }

                sb.AppendLine(root.ToString());

                if (root.ValidationResults != null)
                {
                    for (var i = 0; i < root.ValidationResults.Length; i++)
                    {
                        var child = root.ValidationResults[i];
                        var pretty = PrettyPrint(child, indent, i == root.ValidationResults.Length - 1);
                        sb.Append(pretty);
                    }
                }

                return sb.ToString();
            }
        }
    }
}