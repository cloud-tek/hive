using FluentValidation;

namespace Hive.OpenTelemetry;

/// <summary>
/// FluentValidation-based validator for resource configuration
/// </summary>
internal sealed class ResourceOptionsValidator : AbstractValidator<ResourceOptions>
{
  /// <summary>
  /// Initializes a new instance of the <see cref="ResourceOptionsValidator"/> class
  /// </summary>
  public ResourceOptionsValidator()
  {
    // Resource attribute validation - validate each dictionary entry
    RuleForEach(x => x.Attributes)
      .Must(attr => !string.IsNullOrWhiteSpace(attr.Key))
      .WithMessage("Resource attribute keys cannot be null or whitespace");

    RuleForEach(x => x.Attributes)
      .Must(attr => string.IsNullOrWhiteSpace(attr.Key) || !attr.Key.Any(c => char.IsControl(c)))
      .WithMessage((options, attr) => $"Resource attribute key '{attr.Key}' contains control characters");
  }
}
