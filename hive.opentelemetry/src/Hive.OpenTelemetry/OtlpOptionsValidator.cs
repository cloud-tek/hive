using FluentValidation;

namespace Hive.OpenTelemetry;

/// <summary>
/// FluentValidation-based validator for OTLP exporter configuration
/// </summary>
internal sealed class OtlpOptionsValidator : AbstractValidator<OtlpOptions>
{
  /// <summary>
  /// Initializes a new instance of the <see cref="OtlpOptionsValidator"/> class
  /// </summary>
  public OtlpOptionsValidator()
  {
    // OTLP endpoint validation
    // Validate that endpoint is a valid absolute URI with http/https scheme
    // Uses Custom to ensure only one error is shown per field
    RuleFor(x => x.Endpoint)
      .Custom((endpoint, context) =>
      {
        // Skip validation if endpoint is null or whitespace (optional field)
        if (string.IsNullOrWhiteSpace(endpoint))
          return;

        // Check if it's a valid absolute URI first
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
          context.AddFailure("Endpoint", $"Invalid OTLP endpoint URL '{endpoint}'. Must be a valid absolute URI (e.g., 'http://localhost:4317')");
          return; // Don't check scheme if URI is invalid
        }

        // Then check the scheme (only if URI is valid)
        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
          context.AddFailure("Endpoint", "OTLP endpoint must use http or https scheme");
        }
      });

    // Timeout validation
    RuleFor(x => x.TimeoutMilliseconds)
      .InclusiveBetween(1000, 60000)
      .WithMessage("Timeout must be between 1000 and 60000 milliseconds");

    // Header validation - validate each dictionary entry
    RuleForEach(x => x.Headers)
      .Must(header => !string.IsNullOrWhiteSpace(header.Key))
      .WithMessage("Header keys cannot be null or whitespace");

    RuleForEach(x => x.Headers)
      .Must(header => string.IsNullOrWhiteSpace(header.Key) || !header.Key.Any(c => char.IsControl(c) || c == ',' || c == '='))
      .WithMessage((options, header) => $"Header key '{header.Key}' contains invalid characters (control characters, comma, or equals sign)");

    RuleForEach(x => x.Headers)
      .Must(header => header.Value == null || !header.Value.Any(c => char.IsControl(c) || c == ',' || c == '='))
      .WithMessage((options, header) => $"Header value for '{header.Key}' contains invalid characters (control characters, comma, or equals sign). Per W3C Baggage spec, use percent-encoding for special characters.");
  }
}
