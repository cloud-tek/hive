using Hive.Extensions;
using Microsoft.Extensions.Options;
using MiniValidation;

namespace Hive.Configuration.Validation;

public class MiniOptionsValidator<TOptions>
  : IValidateOptions<TOptions> where TOptions : class
{
  public MiniOptionsValidator()
  {
  }

  public string? Name { get; }

  public ValidateOptionsResult Validate(string? name, TOptions options)
  {
    // Null name is used to configure ALL named options, so always applys.
    if (Name != null && Name != name)
    {
      // Ignored if not validating this instance.
      return ValidateOptionsResult.Skip;
    }

    // Ensure options are provided to validate against
    ArgumentNullException.ThrowIfNull(options);

    // 👇 MiniValidation validation 🎉
    if (MiniValidator.TryValidate(options, out var validationErrors))
    {
      return ValidateOptionsResult.Success;
    }

    var errors = new List<string>();

    foreach (var (key, value) in validationErrors)
    {
      errors.Add(name!= null ? $@"{name}:{key}:{value.ToMultilineString()}" : $"{key}:{value.ToMultilineString()}");
    }

    return ValidateOptionsResult.Fail(errors);
  }
}
