using FluentValidation;
using Hive.Extensions;
using Microsoft.Extensions.Options;

namespace Hive.MicroServices.CORS;

// ReSharper disable once ClassNeverInstantiated.Global
public class OptionsValidator : AbstractValidator<Options>
{
  private readonly IMicroService _service;

  public OptionsValidator(IMicroService service)
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

  public static class Errors
  {
    public const string AllowAnyNotAllowed = "Hive:CORS:AllowAny == 'true' is only permitted in 'Development' environment";
    public const string NoPolicies = "At least 1 Hive:CORS:Policies needs to be defined when Hive:CORS:AllowAny == 'false'";
  }
}
