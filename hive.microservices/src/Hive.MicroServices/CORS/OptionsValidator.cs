using Hive.Extensions;
using Microsoft.Extensions.Options;

namespace Hive.MicroServices.CORS;

// ReSharper disable once ClassNeverInstantiated.Global
public class OptionsValidator : IValidateOptions<Options>
{
  private readonly IMicroService _service;

  public OptionsValidator(IMicroService service)
  {
    _service = service ?? throw new ArgumentNullException(nameof(service));
  }

  public ValidateOptionsResult Validate(string name, Options options)
  {
    if (!_service.Environment.IsDevelopment() && options.AllowAny)
    {
      return ValidateOptionsResult.Fail("Hive:CORS:AllowAny is not allowed in Non-Development environments");
    }

    return ValidateOptionsResult.Success;
  }
}
