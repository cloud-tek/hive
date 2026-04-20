using FluentValidation;

namespace Hive.HTTP.Configuration;

internal sealed class HttpClientRegistrationValidator : AbstractValidator<HttpClientRegistration>
{
  public HttpClientRegistrationValidator()
  {
    RuleFor(r => r.ClientName)
      .NotEmpty();

    RuleFor(r => r.BaseAddress)
      .NotEmpty()
      .WithMessage(r =>
        $"BaseAddress is required for HTTP client '{r.ClientName}'. " +
        $"Provide it via IConfiguration (Hive:Http:{r.ClientName}:BaseAddress) " +
        $"or the fluent API (.WithBaseAddress()).");

    RuleFor(r => r.BaseAddress)
      .Must(addr => Uri.TryCreate(addr, UriKind.Absolute, out _))
      .When(r => !string.IsNullOrEmpty(r.BaseAddress))
      .WithMessage(r =>
        $"BaseAddress '{r.BaseAddress}' for HTTP client '{r.ClientName}' is not a valid absolute URI.");

    RuleFor(r => r)
      .Must(r => r.AuthenticationProviderFactory is not null)
      .When(r => r.AuthenticationType == "BearerToken")
      .WithMessage(r =>
        $"HTTP client '{r.ClientName}' has Authentication.Type 'BearerToken' in configuration " +
        $"but no fluent API .WithAuthentication(auth => auth.BearerToken(...)) was provided. " +
        $"Bearer token authentication requires an async factory delegate.");

    RuleFor(r => r)
      .Must(r => r.AuthenticationProviderFactory is not null)
      .When(r => r.AuthenticationType == "Custom")
      .WithMessage(r =>
        $"HTTP client '{r.ClientName}' has Authentication.Type 'Custom' in configuration " +
        $"but no fluent API .WithAuthentication(auth => auth.Custom(...)) was provided.");

    When(r => r.Resilience.CircuitBreaker is { Enabled: true }, () =>
    {
      RuleFor(r => r.Resilience.CircuitBreaker!.FailureRatio)
        .InclusiveBetween(0.0, 1.0);

      RuleFor(r => r.Resilience.CircuitBreaker!.MinimumThroughput)
        .GreaterThan(0);

      RuleFor(r => r.Resilience.CircuitBreaker!.BreakDuration)
        .GreaterThan(TimeSpan.Zero);
    });
  }
}