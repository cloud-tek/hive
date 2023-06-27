using System.Text;
using FluentValidation.Results;

namespace Hive.Exceptions;

public class CompositeConfigurationException : ConfigurationException
{
  public IEnumerable<ConfigurationException> InnerExceptions { get; init; }
  private readonly ValidationResult _validationResult;

  public CompositeConfigurationException(ValidationResult validationResult, string key)
    : base(validationResult.ToString(), key)
  {
    _validationResult = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
    Key = key ?? throw new ArgumentNullException(nameof(key));
    InnerExceptions = _validationResult.Errors
      .Select(x => new ConfigurationException(x.ErrorCode, $"{key}:{x.PropertyName}")).ToArray();
  }
}
