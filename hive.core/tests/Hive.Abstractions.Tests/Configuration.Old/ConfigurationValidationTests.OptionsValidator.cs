using System;
using Microsoft.Extensions.Options;

namespace Hive.Tests.Configuration;

public partial class ConfigurationValidationTests
{
  public class OptionsValidator : IValidateOptions<Options>
  {
    public ValidateOptionsResult Validate(string? name, Options options)
    {
      throw new NotImplementedException();
    }
  }
}
