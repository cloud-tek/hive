using Serilog;

using Xunit.Abstractions;

namespace Hive.Logging.Xunit;

public static class LoggingConfigurationBuilderExtensions
{
  public static LoggingConfigurationBuilder ToXunit(this LoggingConfigurationBuilder builder, ITestOutputHelper output)
  {
    builder.Sinks.Add((logger, services, microservice) =>
    {

      logger.WriteTo.TestOutput(output);

    });

    builder.Extension.ConfigureRequestLoggingMiddleware += (app) => app.UseSerilogRequestLogging();

    return builder;
  }
}
