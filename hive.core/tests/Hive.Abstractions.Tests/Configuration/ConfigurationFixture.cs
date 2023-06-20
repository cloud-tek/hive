using Microsoft.Extensions.Configuration;

namespace Hive.Tests.Configuration;

public class ConfigurationFixture
{
  protected static IConfigurationRoot GetConfigurationRoot(string config)
  {
    var stream =
      typeof(ConfigurationValidationTests).Assembly.GetManifestResourceStream(
        $"Hive.Tests.Configuration.{config}");
#pragma warning disable CS8604
    return new ConfigurationBuilder().AddJsonStream(stream).Build();
#pragma warning restore CS8604
  }
}
