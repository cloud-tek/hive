using Hive.Extensions;
using Hive.Logging.Enrichers;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hive.Logging;

internal static class LoggerConfigurationExtensions
{
    internal static LoggerConfiguration ConfigureSerilog(
        this LoggerConfiguration loggerConfiguration,
        IMicroService microservice,
        IServiceCollection services,
        IOptions<Options> options,LoggingConfigurationBuilder builder,

        params IDestructuringPolicy[] destructuringPolicies)
    {
        if (options.Value.EnableSelfLog)
        {
            Serilog.Debugging.SelfLog.Enable((msg) =>
            {
                var message = $"SelfLog: {msg}";
                Debug.WriteLine(msg);
                Console.WriteLine(msg);
            });
        }

        return loggerConfiguration
            .ConfigureLogLevel(options)
            .ConfigureSinks(builder, services, microservice)
            .ConfigureEnrichers(microservice)
            .Destructure.With(destructuringPolicies);
    }

    /// <summary>
    /// All logging messages are to be enriched with the enrichers from the below list.
    /// We don't want the developers to have the option to customize this via external configuration.
    /// </summary>
    /// <param name="loggerConfiguration"></param>
    /// <param name="settings">Fasit:Core:Logging config section</param>
    /// <returns></returns>
    internal static LoggerConfiguration ConfigureEnrichers(this LoggerConfiguration loggerConfiguration, IMicroService microservice)
    {
        return loggerConfiguration
            .Enrich.WithProperty("Application", microservice.Name)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.With<ActivityEnricher>()
            .Enrich.With<ExceptionMessageEnricher>()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Environment", microservice.Environment);
    }

    /// <summary>
    /// LogLevel is to be configured from Fasit:Logging:Level IConfiguration key.
    /// Microsoft and ASP.NET Core framework logs are to be suppressed,
    /// to avoid logging the same messages multiple times.
    /// </summary>
    /// <param name="loggerConfiguration"></param>
    /// <param name="settings">Fasit:Core:Logging config section</param>
    /// <returns></returns>
    internal static LoggerConfiguration ConfigureLogLevel(this LoggerConfiguration loggerConfiguration,
        IOptions<Options> options)
    {
        return loggerConfiguration
            .MinimumLevel.Is(options.Value.Level.ToSerilogLogLevel())
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning);
    }

    /// <summary>
    /// Sinks are being configured from the Fasit:Logging:Console and Fasit:Logging:Elasticsearch IConfiguration keys.
    /// This is to be customizable via configuration.
    /// </summary>
    /// <param name="loggerConfiguration"></param>
    /// <param name="settings">Fasit:Core:Logging config section</param>
    /// <returns></returns>
    internal static LoggerConfiguration ConfigureSinks(this LoggerConfiguration loggerConfiguration,
        LoggingConfigurationBuilder builder, IServiceCollection services, IMicroService microservice)
    {
        builder.Sinks.ForEach(sink => sink(loggerConfiguration, services, microservice));

        // if (settings.Console != null && settings.Console.Enabled)
        // {
        //     loggerConfiguration.WriteTo.Async(@async => @async.Console());
        // }

        // if (settings.Elasticsearch != null && settings.Elasticsearch.Enabled)
        // {
        //     settings.Elasticsearch.Validate();
        //
        //     loggerConfiguration.WriteTo.Async(@async => @async.Elasticsearch(
        //         new ElasticsearchSinkOptions(settings.Elasticsearch.NodeUris.Select(uri => new Uri(uri)))
        //         {
        //             IndexFormat = settings.Elasticsearch.IsSharedLocalInstance
        //                 ? $"app-local-{Environment.GetEnvironmentVariable("USERNAME")?.ToLower(CultureInfo.InvariantCulture) ?? "anonymous"}-tracelogs-{{0:yyyy.MM.dd}}"
        //                 : $"app-{HostingEnvironment.Name.ToLower(CultureInfo.InvariantCulture)}-tracelogs-{{0:yyyy.MM.dd}}",
        //             TemplateName = "app-log-template-v6",
        //             EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog,
        //             ModifyConnectionSettings = connection =>
        //             {
        //                 if (settings.Elasticsearch.UseAuthentication)
        //                 {
        //                     connection.BasicAuthentication(
        //                         settings.Elasticsearch.Username,
        //                         settings.Elasticsearch.Password);
        //                 }
        //
        //                 return connection;
        //             }
        //         }));
        // }

        return loggerConfiguration;
    }
}
