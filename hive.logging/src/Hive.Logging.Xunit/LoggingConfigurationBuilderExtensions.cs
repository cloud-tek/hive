using System.Globalization;
using Serilog;

using Xunit.Abstractions;

namespace Hive.Logging.Xunit;

/// <summary>
/// Provides extension methods for <see cref="LoggingConfigurationBuilder"/>.
/// </summary>
public static class LoggingConfigurationBuilderExtensions
{
  /// <summary>
  /// Configures the logger to write to the xunit output.
  /// </summary>
  /// <param name="builder"></param>
  /// <param name="output"></param>
  /// <returns><see cref="LoggingConfigurationBuilder"/></returns>
  public static LoggingConfigurationBuilder ToXunit(this LoggingConfigurationBuilder builder, ITestOutputHelper output)
  {
    builder.Sinks.Add((logger, services, microservice) =>
    {
      logger.WriteTo.TestOutput(output, formatProvider: CultureInfo.InvariantCulture);
    });

    builder.Extension.ConfigureRequestLoggingMiddleware += (app) => app.UseSerilogRequestLogging();

    return builder;
  }
}