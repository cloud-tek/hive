using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Hive.Testing;

/// <summary>
/// A helper class for extending <see cref="IConfigurationBuilder"/>
/// </summary>
public static class ConfigurationExtensions
{
  private const string HiveLoggingLogzIoEnvVar = "Hive__Logging__LogzIo__Token";

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
      { "Hive:Logging:Level", "Information" }
    });

    return builder;
  }

  /// <summary>
  /// Adds the test Logz.io configuration to the configuration builder
  /// </summary>
  /// <param name="builder"></param>
  /// <returns><see cref="IConfigurationBuilder"/></returns>
  /// <exception cref="ArgumentNullException">Thrown when the provided argument is null</exception>
  public static IConfigurationBuilder UseTestLogzIoConfiguration(this IConfigurationBuilder builder)
  {
#pragma warning disable CA2208, MA0015
    builder.AddInMemoryCollection(new Dictionary<string, string?>()
    {
      { "Hive:Logging:LogzIo:Region", "eu" },
      {
        "Hive:Logging:LogzIo:Token",
        Environment.GetEnvironmentVariable(HiveLoggingLogzIoEnvVar) ?? throw new ArgumentNullException($"Missing environment variable {HiveLoggingLogzIoEnvVar}")
      }
    });
#pragma warning restore CA2208, MA0015
    return builder;
  }
}