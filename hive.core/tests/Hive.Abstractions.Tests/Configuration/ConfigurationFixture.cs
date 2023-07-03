using System.Collections.Immutable;
using Hive.Testing;
using Microsoft.Extensions.Configuration;

namespace Hive.Tests.Configuration;

public class ConfigurationFixture
{
  protected static IConfigurationRoot GetConfigurationRoot(string config)
  {
    return new ConfigurationBuilder()
      .UseEmbeddedConfiguration(typeof(SimpleOptions).Assembly, "Hive.Tests.Configuration", config)
      .Build();
//     var stream =
//       typeof(SimpleOptions).Assembly.GetManifestResourceStream(
//         $"Hive.Tests.Configuration.{config}");
// #pragma warning disable CS8604
//     return new ConfigurationBuilder().AddJsonStream(stream).Build();
// #pragma warning restore CS8604
  }
}
