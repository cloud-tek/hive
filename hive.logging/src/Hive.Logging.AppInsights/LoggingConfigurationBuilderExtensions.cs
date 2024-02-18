using Hive.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Hive.Logging.AppInsights;

/// <summary>
/// Provides a simple way to configure logging.
/// </summary>
public static class LoggingConfigurationBuilderExtensions
{
  /// <summary>
  /// Configures the logger to write to Application Insights.
  /// </summary>
  /// <param name="builder"></param>
  /// <returns><see cref="LoggingConfigurationBuilder"/></returns>
  public static LoggingConfigurationBuilder ToAppInsights(this LoggingConfigurationBuilder builder)
  {
    builder.Sinks.Add((logger, services, microservice) =>
    {
      var options = services.PreConfigureOptions<Options>(microservice.ConfigurationRoot, () => Options.SectionKey);
      services.AddSingleton<RequestLoggingMiddleware>();

      var client = new TelemetryClient(options.Value.ToTelemetryConfiguration());

      logger.WriteTo.ApplicationInsights(client, TelemetryConverter.Traces);
    });

    builder.Extension.ConfigureRequestLoggingMiddleware += (app) => app.UseMiddleware<RequestLoggingMiddleware>();

    return builder;
  }
}