using FluentValidation;
using Hive.Extensions;

namespace Hive.Configuration.CORS;

/// <summary>
/// CORS OptionsValidator based on FluentValidation
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class OptionsValidator : AbstractValidator<Options>
{
  /// <summary>
  /// Initializes a new instance of the <see cref="OptionsValidator"/> class.
  /// </summary>
  /// <param name="service"></param>
  public OptionsValidator(IMicroServiceCore service)
  {
    _ = service ?? throw new ArgumentNullException(nameof(service));

    RuleFor(x => x.AllowAny)
      .Must(_ => service.Environment.IsDevelopment())
      .When(x => x.AllowAny == true)
      .WithMessage(Errors.AllowAnyNotAllowed);

    RuleFor(x => x.Policies)
      .NotNull()
      .When(x => x.AllowAny == false)
      .WithMessage(Errors.NoPolicies);

    RuleForEach(x => x.Policies).SetValidator(new CORSPolicyValidator());
  }

  /// <summary>
  /// Static class containing error messages
  /// </summary>
  public static class Errors
  {
    /// <summary>
    /// A CORS policy that allows ANY should not be permitted outside of the DEV environment
    /// </summary>
    public const string AllowAnyNotAllowed = "Hive:CORS:AllowAny == 'true' is only permitted in 'Development' environment";

    /// <summary>
    /// An error indicating that no CORS policies have been defined
    /// </summary>
    public const string NoPolicies = "At least 1 Hive:CORS:Policies needs to be defined when Hive:CORS:AllowAny == 'false'";
  }
}