using Hive.Configuration;
using Hive.Configuration.CORS;
using Hive.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CORSOptions = Hive.Configuration.CORS.Options;

namespace Hive.MicroServices.CORS;

/// <summary>
/// CORS extension for the microservice
/// </summary>
public class Extension : MicroServiceExtension<Extension>
{
  private const string AllowAnyPolicyName = "Allow Any";

  /// <summary>
  /// The name of the CORS policy to apply
  /// </summary>
  public string? PolicyName { get; private set; }

  /// <summary>
  /// Create a new instance of the extension
  /// </summary>
  /// <param name="service"></param>
  public Extension(IMicroServiceCore service) : base(service)
  {
  }

  /// <summary>
  /// Configure the service
  /// </summary>
  /// <param name="services"></param>
  /// <param name="microservice"></param>
  /// <returns><see cref="IServiceCollection"/></returns>
  public override IServiceCollection ConfigureServices(IServiceCollection services, IMicroServiceCore microservice)
  {
    ConfigureActions.Add((svc, configuration) =>
    {
      var validator = new Hive.Configuration.CORS.OptionsValidator(microservice);
      try
      {
        var options =
          svc.PreConfigureValidatedOptions<CORSOptions, Hive.Configuration.CORS.OptionsValidator>(
            configuration,
            validator,
            () => CORSOptions.SectionKey);

        svc.AddCors(cors =>
        {
          if (options.Value.AllowAny)
          {
            PolicyName = AllowAnyPolicyName;

            // Set as default policy so it applies to all endpoints automatically
            cors.AddDefaultPolicy(policy =>
            {
              policy.AllowAnyHeader();
              policy.AllowAnyMethod();
              policy.AllowAnyOrigin();
            });

            ((MicroService)Service).Logger.LogInformationPolicyConfigured(AllowAnyPolicyName);
          }
          else
          {
            // Validate that policies collection is not empty
            if (options.Value.Policies == null || options.Value.Policies.Length == 0)
            {
              throw new InvalidOperationException("CORS policies collection cannot be empty when AllowAny is false");
            }

            // Use the first policy as the default policy
            PolicyName = options.Value.Policies[0].Name;

            // Set first policy as default
            var firstPolicy = options.Value.Policies[0];
            cors.AddDefaultPolicy(builder => BuildCorsPolicy(builder, firstPolicy));
            ((MicroService)Service).Logger.LogInformationPolicyConfigured($"{firstPolicy.Name} (default)");

            // Register remaining policies by name
            // Skip(1) excludes the first policy since it's already registered as the default above
            options.Value.Policies.Skip(1).ForEach(policy =>
            {
              cors.AddPolicy(policy.Name, builder => BuildCorsPolicy(builder, policy));
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

  /// <summary>
  /// Builds a CORS policy from the configuration
  /// </summary>
  /// <param name="builder">The CORS policy builder</param>
  /// <param name="policy">The CORS policy configuration</param>
  private static void BuildCorsPolicy(CorsPolicyBuilder builder, CORSPolicy policy)
  {
    if (policy.AllowedHeaders != null && policy.AllowedHeaders.Length > 0)
    {
      builder.WithHeaders(policy.AllowedHeaders);
    }

    if (policy.AllowedOrigins != null && policy.AllowedOrigins.Length > 0)
    {
      builder.WithOrigins(policy.AllowedOrigins);
    }

    if (policy.AllowedMethods != null && policy.AllowedMethods.Length > 0)
    {
      builder.WithMethods(policy.AllowedMethods);
    }
  }
}

internal static partial class ExtensionLogMessages
{
  [LoggerMessage((int)MicroServiceLogEventId.ServiceExtensionConfigurationApplied, LogLevel.Information, "Hive:CORS policy {name} configured")]
  internal static partial void LogInformationPolicyConfigured(this ILogger logger, string name);

  [LoggerMessage((int)MicroServiceLogEventId.ServiceExtensionCriticalFailure, LogLevel.Critical, "Hive:CORS validation failed")]
  internal static partial void LogCriticalValidationFailed(this ILogger logger, OptionsValidationException exception);
}