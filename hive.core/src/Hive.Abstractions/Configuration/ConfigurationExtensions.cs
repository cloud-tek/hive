using Hive.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Hive.Configuration;

/// <summary>
/// Extensions for <see cref="IConfiguration"/>
/// </summary>
public static class ConfigurationExtensions
{
  /// <summary>
  /// Gets an existing section from the configuration
  /// </summary>
  /// <param name="configuration"></param>
  /// <param name="key"></param>
  /// <returns><see cref="IConfigurationSection"/>></returns>
  public static IConfigurationSection GetExistingSection(this IConfiguration configuration, string key)
  {
    var configurationSection = configuration.GetSection(key);

    if (!configurationSection.Exists())
    {
      throw configuration switch
      {
        IConfigurationRoot configurationIsRoot => new ConfigurationException($"Section with key '{key}' does not exist", key),
        IConfigurationSection configurationIsSection => new ConfigurationException($"Section with key '{key}' does not exist at '{configurationIsSection.Path}'. Expected configuration path is '{configurationSection.Path}'", key),
        _ => new ConfigurationException($"Failed to find configuration at '{configurationSection.Path}'", key)
      };
    }

    return configurationSection;
  }
}