using Microsoft.Extensions.DependencyInjection;
using Hive.Configuration;

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
      var options = svc.PreConfigureOptions<Options>(configuration, () => Options.SectionKey);
    });

    //services.ConfigureValidatedOptions<Options, OptionsValidator>(microservice.ConfigurationRoot, () => Options.SectionKey);


    //throw new NotImplementedException();
    // if(options)
    //   services.AddCors(cors =>
    //   {
    //     cors.AddPolicy(name: "AllowSpecificOrigins", policy =>
    //     {
    //       policy.WithHeaders();
    //       policy.WithMethods();
    //       policy.WithOrigins(urls);
    //       // policy.WithOrigins("https://*.example.com")
    //       //   .SetIsOriginAllowedToAllowWildcardSubdomains();
    //
    //       // policy.WithOrigins("https://*.example.com")
    //       //   .WithExposedHeaders("x-custom-header");
    //     });
    //   });

    return services;
  }
}
