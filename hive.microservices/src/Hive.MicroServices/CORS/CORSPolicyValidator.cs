using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Hive.Extensions;

namespace Hive.MicroServices.CORS;

public class CORSPolicyValidator : AbstractValidator<CORSPolicy>
{
  private static readonly string[] AllowedHeaders = new[] { "GET", "POST", "PUT", "PATCH", "DELETE" };
  public CORSPolicyValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty()
      .WithMessage(Errors.NameRequired);

    RuleFor(x => new { x.AllowedOrigins, x.AllowedMethods, x.AllowedHeaders })
      .Must(x => ValidatePolicy(x.AllowedOrigins, x.AllowedMethods, x.AllowedHeaders))
      .WithMessage(Errors.PolicyEmpty);

    RuleForEach(x => x.AllowedOrigins)
      .Must(value => Uri.TryCreate(value, UriKind.Absolute, out _))
      .When(x => !x.AllowedOrigins.IsNullOrEmpty())
      .WithMessage(Errors.AllowedOriginsInvalidFormat);

    RuleForEach(x => x.AllowedMethods)
      .Must(value => AllowedHeaders.Contains(value))
      .When(x => !x.AllowedMethods.IsNullOrEmpty())
      .WithMessage(Errors.AllowedMethodsInvalidValue);
  }

  private bool ValidatePolicy(string[] allowedOrigins, string[] allowedMethods, string[] allowedHeaders)
  {
    return !(allowedOrigins.IsNullOrEmpty() && allowedMethods.IsNullOrEmpty() && allowedHeaders.IsNullOrEmpty());
  }

  public static class Errors
  {
    public const string NameRequired = "Hive:CORS:Policies[]:Name is required";
    public const string PolicyEmpty = "At least one of Hive:Cors:Policies[]:AllowedOrigins, Hive:Cors:Policies[]:AllowedHeaders, Hive:Cors:Policies[]:AllowedMethods must not be empty";
    public const string AllowedOriginsInvalidFormat = "At least one of Hive:Cors:Policies[]:AllowedOrigins has an invalid format";
    public const string AllowedMethodsInvalidValue = "At least one of Hive:Cors:Policies[]:AllowedMethods has an invalid value";
  }
}
