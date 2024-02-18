using System.Collections.Immutable;
using Hive.Testing;
using Microsoft.Extensions.Configuration;

namespace Hive.Tests.Configuration;

public class ConfigurationFixture
{
  protected static IConfigurationRoot GetConfigurationRoot(string config)
  {
    return new ConfigurationBuilder()
      .UseEmbeddedConfiguration(typeof(SimpleOptions).Assembly, $"{typeof(SimpleOptions).Assembly.GetName().Name}.Configuration", config)
      .Build();
  }
}