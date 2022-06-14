using Ion.Configuration;
using Serilog;
using Serilog.Sinks.Logz.Io;

namespace Ion.Logging.LogzIo;

public static class LoggingConfigurationBuilderExtensions
{
    public static LoggingConfigurationBuilder ToLogzIo(this LoggingConfigurationBuilder builder)
    {
        builder.Sinks.Add((logger, services, microservice) =>
        {
            var settings = services.ConfigureOptions<Options>(microservice.ConfigurationRoot, () => Options.SectionKey);
            string subdomain = null;

            switch (settings.Region)
            {
                case "eu":
                    subdomain = "listener-eu";
                    break;
                case "us":
                    subdomain = "listener";
                    break;
                default:
                    throw new NotImplementedException($"Unsupported logz.io region: {settings.Region}");
            }

            logger.WriteTo.LogzIoDurableHttp(
                $"https://{subdomain}.logz.io:8071/?type=app&token={settings.Token}",
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