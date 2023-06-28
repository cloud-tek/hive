using System.Text;
using FluentValidation.Results;
using Hive.Extensions;

namespace Hive.Exceptions;

public class CompositeConfigurationException : ConfigurationException
{
  public IEnumerable<ConfigurationException> InnerExceptions { get; init; }

  public CompositeConfigurationException(ValidationResult validationResult, string key)
    : base(validationResult.ToString(), key)
  {
    _ = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
    Key = key ?? throw new ArgumentNullException(nameof(key));

    InnerExceptions = validationResult.Errors
      .Select(x => new ConfigurationException(x.ErrorCode, $"{key}:{x.PropertyName}")).ToArray();
  }

  public CompositeConfigurationException(IDictionary<string, string[]> errors, string key)
  {
    _ = errors ?? throw new ArgumentNullException(nameof(errors));
    Key = key ?? throw new ArgumentNullException(nameof(key));

    InnerExceptions = errors
      .Select(kvp => new ConfigurationException(kvp.Value.ToMultilineString(), $"{key}:{kvp.Key}")).ToArray();
  }
}
