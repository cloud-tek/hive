using Microsoft.Extensions.Configuration;

namespace Hive.Configuration;

/// <summary>
/// Factory for creating a standard <see cref="IConfigurationBuilder"/> with shared configuration files.
/// Provides a consistent configuration loading pattern across all Hive host types.
/// </summary>
public static class ConfigurationBuilderFactory
{
  /// <summary>
  /// Creates a new <see cref="IConfigurationBuilder"/> with the standard Hive configuration file hierarchy.
  /// </summary>
  /// <param name="environment">The current environment name (e.g., "dev", "production")</param>
  /// <returns>A configured <see cref="IConfigurationBuilder"/></returns>
  public static IConfigurationBuilder CreateDefault(string environment)
  {
    return new ConfigurationBuilder()
      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
      .AddJsonFile($"appsettings.{environment}.json", optional: true)
      .AddSharedConfiguration(environment)
      .AddEnvironmentVariables();
  }

  /// <summary>
  /// Adds the Hive shared configuration files to an existing <see cref="IConfigurationBuilder"/>.
  /// Use this when adding shared config to a builder that already has appsettings.json and environment variables.
  /// </summary>
  /// <param name="builder">The existing configuration builder</param>
  /// <param name="environment">The current environment name (e.g., "dev", "production")</param>
  /// <returns>The <see cref="IConfigurationBuilder"/> for chaining</returns>
  public static IConfigurationBuilder AddSharedConfiguration(this IConfigurationBuilder builder, string environment)
  {
    return builder
      .AddJsonFile("appsettings.shared.json", optional: true)
      .AddJsonFile($"appsettings.shared.{environment}.json", optional: true);
  }
}