using Hive.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Hive.Logging.AppInsights;

public static class LoggingConfigurationBuilderExtensions
{
    public static LoggingConfigurationBuilder ToAppInsights(this LoggingConfigurationBuilder builder)
    {
        builder.Sinks.Add((logger, services, microservice) =>
        {
            var options = services.ConfigureOptions<Options>(microservice.ConfigurationRoot, () => Options.SectionKey);
            services.AddSingleton<RequestLoggingMiddleware>();

            var client = new TelemetryClient(options.ToTelemetryConfiguration());

            logger.WriteTo.ApplicationInsights(client, TelemetryConverter.Traces);
        });

        builder.Extension.ConfigureRequestLoggingMiddleware += (app) => app.UseMiddleware<RequestLoggingMiddleware>();

        return builder;
    }
}