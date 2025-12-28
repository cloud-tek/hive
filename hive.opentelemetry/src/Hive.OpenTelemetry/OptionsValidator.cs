using FluentValidation;

namespace Hive.OpenTelemetry;

/// <summary>
/// FluentValidation-based validator for OpenTelemetry configuration options
/// </summary>
internal sealed class OptionsValidator : AbstractValidator<OpenTelemetryOptions>
{
  /// <summary>
  /// Initializes a new instance of the <see cref="OptionsValidator"/> class
  /// </summary>
  public OptionsValidator()
  {
    // Use child validators for nested options
    RuleFor(x => x.Otlp).SetValidator(new OtlpOptionsValidator());
    RuleFor(x => x.Resource).SetValidator(new ResourceOptionsValidator());
  }
}