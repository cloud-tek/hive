using Microsoft.Extensions.Options;

namespace Hive.Abstractions.Tests.Configuration;

public class SimpleOptionsValidator : IValidateOptions<SimpleOptions>
{
  public ValidateOptionsResult Validate(string? name, SimpleOptions options)
  {
    if (string.IsNullOrEmpty(options.Name))
    {
      return ValidateOptionsResult.Fail("SimpleOptions:Name is required");
    }

    if (options.Name.Length < 3)
    {
      return ValidateOptionsResult.Fail("SimpleOptions:Name minimum length is 3 characters");
    }

    return ValidateOptionsResult.Success;
  }
}