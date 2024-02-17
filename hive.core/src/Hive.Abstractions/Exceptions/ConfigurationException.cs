namespace Hive.Exceptions;

/// <summary>
/// Exception thrown when there is an error in the configuration
/// </summary>
public class ConfigurationException : Exception
{
  public string? Key { get; protected init; } = default!;

  /// <summary>
  /// Creates a new instance of <see cref="ConfigurationException"/>
  /// </summary>
  /// <param name="message"></param>
  public ConfigurationException(string? message)
      : base(message)
  {
  }

  /// <summary>
  /// Creates a new instance of <see cref="ConfigurationException"/>
  /// </summary>
  /// <param name="message"></param>
  /// <param name="key"></param>
  public ConfigurationException(string? message, string? key)
      : base(message)
  {
    Key = key;
  }

  /// <summary>
  /// Creates a new instance of <see cref="ConfigurationException"/>
  /// </summary>
  protected ConfigurationException()
  { }
}