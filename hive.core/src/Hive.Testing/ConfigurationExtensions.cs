using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Reflection;

namespace Hive.Testing;

public static class ConfigurationExtensions
{
  private const string HiveLoggingLogzIoEnvVar = "Hive__Logging__LogzIo__Token";

  public static IConfigurationBuilder UseEmbeddedConfiguration(this IConfigurationBuilder builder, Assembly assembly,
    string embeddedPath,
    params string[] configs)
  {
    foreach (var config in configs)
    {
      var path = string.IsNullOrEmpty(embeddedPath) ? config : $"{embeddedPath}.{config}";
      var stream = assembly.GetManifestResourceStream(path);
      builder.AddJsonStream(stream);
    }

    return builder;
  }

  public static IConfigurationBuilder UseDefaultLoggingConfiguration(this IConfigurationBuilder builder)
  {
    builder.AddInMemoryCollection(new Dictionary<string, string>()
    {
      { "Hive:Logging:Level", "Information" }
    });

    return builder;
  }

  public static IConfigurationBuilder UseTestLogzIoConfiguration(this IConfigurationBuilder builder)
  {
    builder.AddInMemoryCollection(new Dictionary<string, string>()
    {
      { "Hive:Logging:LogzIo:Region", "eu" },
      {
        "Hive:Logging:LogzIo:Token",
        Environment.GetEnvironmentVariable(HiveLoggingLogzIoEnvVar) ??
        throw new ArgumentNullException($"Missing environment variable {HiveLoggingLogzIoEnvVar}")
      }
    });

    return builder;
  }
}
