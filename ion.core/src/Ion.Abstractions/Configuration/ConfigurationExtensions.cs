using Ion.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Ion.Configuration;

public static class ConfigurationExtensions
{
    public static IConfigurationSection GetExistingSection(this IConfiguration configuration, string key)
    {
        var configurationSection = configuration.GetSection(key);

        if (!configurationSection.Exists())
        {
            throw configuration switch
            {
                IConfigurationRoot configurationIsRoot => new ConfigurationException($"Section with key '{key}' does not exist. Existing values are: {configurationIsRoot.GetDebugView()}", key),
                IConfigurationSection configurationIsSection => new ConfigurationException($"Section with key '{key}' does not exist at '{configurationIsSection.Path}'. Expected configuration path is '{configurationSection.Path}'", key),
                _ => new ConfigurationException($"Failed to find configuration at '{configurationSection.Path}'", key)
            };
        }

        return configurationSection;
    }
}