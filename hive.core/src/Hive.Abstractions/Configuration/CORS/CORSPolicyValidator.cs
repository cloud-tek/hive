using FluentValidation;
using Hive.Extensions;

namespace Hive.Configuration.CORS;

/// <summary>
/// The CORS policy validator.
/// </summary>
public class CORSPolicyValidator : AbstractValidator<CORSPolicy>
{
  private static readonly string[] ValidHttpMethods = new[] { "GET", "POST", "PUT", "PATCH", "DELETE" };

  /// <summary>
  /// Creates a new <see cref="CORSPolicyValidator"/> instance.
  /// </summary>
  public CORSPolicyValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty()
      .WithMessage(Errors.NameRequired);

    RuleFor(x => new { x.AllowedOrigins, x.AllowedMethods, x.AllowedHeaders })
      .Must(x => ValidatePolicy(x.AllowedOrigins, x.AllowedMethods, x.AllowedHeaders))
      .WithMessage(Errors.PolicyEmpty);

    RuleForEach(x => x.AllowedOrigins)
      .Must(value => value == "*" || Uri.TryCreate(value, UriKind.Absolute, out _))
      .When(x => !x.AllowedOrigins.IsNullOrEmpty())
      .WithMessage(Errors.AllowedOriginsInvalidFormat);

    RuleForEach(x => x.AllowedMethods)
      .Must(value => ValidHttpMethods.Contains(value))
      .When(x => !x.AllowedMethods.IsNullOrEmpty())
      .WithMessage(Errors.AllowedMethodsInvalidValue);
  }

  /// <summary>
  /// Validates the policy
  /// </summary>
  /// <param name="allowedOrigins"></param>
  /// <param name="allowedMethods"></param>
  /// <param name="allowedHeaders"></param>
  /// <returns>boolean</returns>
  private static bool ValidatePolicy(string[] allowedOrigins, string[] allowedMethods, string[] allowedHeaders)
  {
    return !(allowedOrigins.IsNullOrEmpty() && allowedMethods.IsNullOrEmpty() && allowedHeaders.IsNullOrEmpty());
  }

  /// <summary>
  /// CORSPolicy validation errors
  /// </summary>
  public static class Errors
  {
    /// <summary>
    /// Name required error
    /// </summary>
    public const string NameRequired = "Hive:CORS:Policies[]:Name is required";

    /// <summary>
    /// Policy empty error
    /// </summary>
    public const string PolicyEmpty = "At least one of Hive:Cors:Policies[]:AllowedOrigins, Hive:Cors:Policies[]:AllowedHeaders, Hive:Cors:Policies[]:AllowedMethods must not be empty";

    /// <summary>
    /// Allowed origins invalid format error
    /// </summary>
    public const string AllowedOriginsInvalidFormat = "At least one of Hive:Cors:Policies[]:AllowedOrigins has an invalid format";

    /// <summary>
    /// Allowed methods invalid value error
    /// </summary>
    public const string AllowedMethodsInvalidValue = "At least one of Hive:Cors:Policies[]:AllowedMethods has an invalid value";
  }
}