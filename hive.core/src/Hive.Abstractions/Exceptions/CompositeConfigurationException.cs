using FluentValidation.Results;
using Hive.Extensions;

namespace Hive.Exceptions;

/// <summary>
/// Exception thrown when there are multiple configuration errors
/// </summary>
public class CompositeConfigurationException : ConfigurationException
{
  /// <summary>
  /// The inner exceptions
  /// </summary>
  public IEnumerable<ConfigurationException> InnerExceptions { get; init; }

  /// <summary>
  /// Creates a new instance of <see cref="CompositeConfigurationException"/>
  /// </summary>
  /// <param name="validationResult"></param>
  /// <param name="key"></param>
  /// <exception cref="ArgumentNullException">When any of the provided parameters is null (or empty)</exception>
  public CompositeConfigurationException(ValidationResult validationResult, string key)
    : base(validationResult.ToString(), key)
  {
    _ = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
    Key = key ?? throw new ArgumentNullException(nameof(key));

    InnerExceptions = validationResult.Errors
      .Select(x => new ConfigurationException(x.ErrorCode, $"{key}:{x.PropertyName}")).ToArray();
  }

  /// <summary>
  /// Creates a new instance of <see cref="CompositeConfigurationException"/>
  /// </summary>
  /// <param name="errors"></param>
  /// <param name="key"></param>
  /// <exception cref="ArgumentNullException">When any of the provided parameters is null (or empty)</exception>
  public CompositeConfigurationException(IDictionary<string, string[]> errors, string key)
  {
    _ = errors ?? throw new ArgumentNullException(nameof(errors));
    Key = key ?? throw new ArgumentNullException(nameof(key));

    InnerExceptions = errors
      .Select(kvp => new ConfigurationException(kvp.Value.ToMultilineString(), $"{key}:{kvp.Key}")).ToArray();
  }
}