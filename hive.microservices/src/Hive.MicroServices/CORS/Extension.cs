using Hive.Configuration;
using Hive.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hive.MicroServices.CORS;

/// <summary>
/// CORS extension for the microservice
/// </summary>
public class Extension : MicroServiceExtension
{
  /// <summary>
  /// Create a new instance of the extension
  /// </summary>
  /// <param name="service"></param>
  public Extension(IMicroService service) : base(service)
  {
  }

  /// <summary>
  /// Configure the service
  /// </summary>
  /// <param name="services"></param>
  /// <param name="microservice"></param>
  /// <returns><see cref="IServiceCollection"/></returns>
  public override IServiceCollection ConfigureServices(IServiceCollection services, IMicroService microservice)
  {
    ConfigureActions.Add((svc, configuration) =>
    {
      var validator = new OptionsValidator(microservice);
      try
      {
        var options =
          svc.PreConfigureValidatedOptions<Options, OptionsValidator>(
            configuration,
            validator,
            () => Options.SectionKey);

        svc.AddCors(cors =>
        {
          if (options.Value.AllowAny)
          {
            const string name = "Allow Any";
            cors.AddPolicy(name, (policy) =>
            {
              policy.AllowCredentials();
              policy.AllowAnyHeader();
              policy.AllowAnyMethod();
              policy.AllowAnyOrigin();
            });
            ((MicroService)Service).Logger.LogInformationPolicyConfigured(name);
          }
          else
          {
            options.Value.Policies.ForEach(policy =>
            {
              cors.AddPolicy(policy.Name, x => policy.ToCORSPolicyBuilderAction());
              ((MicroService)Service).Logger.LogInformationPolicyConfigured(policy.Name);
            });
          }
        });
      }
      catch (OptionsValidationException oex)
      {
        ((MicroService)Service).Logger.LogCriticalValidationFailed(oex);
        throw;
      }
    });

    return services;
  }
}

internal static partial class ExtensionLogMessages
{
  [LoggerMessage((int)MicroServiceLogEventId.ServiceExtensionConfigurationApplied, LogLevel.Information, "Hive:CORS policy {name} configured")]
  internal static partial void LogInformationPolicyConfigured(this ILogger logger, string name);

  [LoggerMessage((int)MicroServiceLogEventId.ServiceExtensionCriticalFailure, LogLevel.Critical, "Hive:CORS validation failed")]
  internal static partial void LogCriticalValidationFailed(this ILogger logger, OptionsValidationException exception);
}