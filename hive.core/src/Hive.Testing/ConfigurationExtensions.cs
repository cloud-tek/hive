using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Hive.Testing;

/// <summary>
/// A helper class for extending <see cref="IConfigurationBuilder"/>
/// </summary>
public static class ConfigurationExtensions
{
  /// <summary>
  /// Adds the embedded configuration to the configuration builder
  /// </summary>
  /// <param name="builder"></param>
  /// <param name="assembly"></param>
  /// <param name="embeddedPath"></param>
  /// <param name="configs"></param>
  /// <returns><see cref="IConfigurationBuilder"/></returns>
  public static IConfigurationBuilder UseEmbeddedConfiguration(
    this IConfigurationBuilder builder,
    Assembly assembly,
    string embeddedPath,
    params string[] configs)
  {
    foreach (var config in configs)
    {
      var path = string.IsNullOrEmpty(embeddedPath) ? config : $"{embeddedPath}.{config}";
      var stream = assembly.GetManifestResourceStream(path);
      builder.AddJsonStream(stream!);
    }

    return builder;
  }

  /// <summary>
  /// Adds the default logging configuration to the configuration builder
  /// </summary>
  /// <param name="builder"></param>
  /// <returns><see cref="IConfigurationBuilder"/></returns>
  public static IConfigurationBuilder UseDefaultLoggingConfiguration(this IConfigurationBuilder builder)
  {
    builder.AddInMemoryCollection(new Dictionary<string, string?>()
    {
      { "Logging:LogLevel:Default", "Information" }
    });

    return builder;
  }
}