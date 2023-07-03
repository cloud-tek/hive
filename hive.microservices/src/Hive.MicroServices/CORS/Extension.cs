using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Hive.Configuration;
using Hive.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hive.MicroServices.CORS;

public class Extension : MicroServiceExtension
{
  public Extension(IMicroService service) : base(service)
  {
  }

  public override IServiceCollection ConfigureServices(IServiceCollection services, IMicroService microservice)
  {
    this.ConfigureActions.Add((svc, configuration) =>
    {
      var validator = new OptionsValidator(microservice);
      try
      {
        var options =
          svc.PreConfigureValidatedOptions<Options, OptionsValidator>(configuration, validator,
            () => Options.SectionKey);

        svc.AddCors(cors =>
        {
          var sb = new StringBuilder("Configuring Hive:CORS policies...");
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
            sb.AppendLine(name);
          }
          else
          {
            options.Value.Policies.ForEach(policy =>
            {
              cors.AddPolicy(policy.Name, x => policy.ToCORSPolicyBuilderAction());
              sb.AppendLine(policy.Name);
            });
          }
          ((MicroService)Service).Logger.LogInformation(sb.ToString());
        });
      }
      catch (OptionsValidationException oex)
      {
        ((MicroService)Service).Logger.LogCritical(oex, "Hive:CORS validation failed");
        throw;
      }
    });

    return services;
  }
}
