using Hive.Configuration;
using Serilog;
using Serilog.Sinks.Logz.Io;

namespace Hive.Logging.LogzIo;

/// <summary>
/// Provides extension methods for <see cref="LoggingConfigurationBuilder"/>.
/// </summary>
public static class LoggingConfigurationBuilderExtensions
{
  /// <summary>
  /// Configures the logger to write to logz.io.
  /// </summary>
  /// <param name="builder"></param>
  /// <returns><see cref="LoggingConfigurationBuilder"/></returns>
  /// <exception cref="NotSupportedException">When an unknown (non eu | us) LogzIo region is configured</exception>
  public static LoggingConfigurationBuilder ToLogzIo(this LoggingConfigurationBuilder builder)
  {
    builder.Sinks.Add((logger, services, microservice) =>
    {
      var options = services.PreConfigureOptions<Options>(microservice.ConfigurationRoot, () => Options.SectionKey);
      string? subdomain = null;

      switch (options.Value.Region)
      {
        case "eu":
          subdomain = "listener-eu";
          break;
        case "us":
          subdomain = "listener";
          break;
        default:
          throw new NotSupportedException($"Unsupported logz.io region: {options.Value.Region}");
      }

      logger.WriteTo.LogzIoDurableHttp(
              $"https://{subdomain}.logz.io:8071/?type=app&token={options.Value.Token}",
              logzioTextFormatterOptions: new LogzioTextFormatterOptions
              {
                BoostProperties = true,
                IncludeMessageTemplate = true,
                LowercaseLevel = true,
              });
    });

    builder.Extension.ConfigureRequestLoggingMiddleware += (app) => app.UseSerilogRequestLogging();

    return builder;
  }
}