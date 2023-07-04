using Hive.Configuration;
using Serilog;
using Serilog.Sinks.Logz.Io;

namespace Hive.Logging.LogzIo;

public static class LoggingConfigurationBuilderExtensions
{
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
                    throw new NotImplementedException($"Unsupported logz.io region: {options.Value.Region}");
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
