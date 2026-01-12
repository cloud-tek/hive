using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Hive.MicroServices.Api
{
  /// <summary>
  /// Extension methods for <see cref="IMicroService"/>
  /// </summary>
  public static class IMicroServiceExtensions
  {
    /// <summary>
    /// Configures the default minimal API pipeline for the microservice
    /// </summary>
    /// <param name="microservice"></param>
    /// <param name="action"></param>
    /// <returns><see cref="IMicroService"/></returns>
    public static IMicroService ConfigureApiPipeline(this IMicroService microservice, Action<Microsoft.AspNetCore.Routing.IEndpointRouteBuilder> action)
    {
      var service = (MicroService)microservice;

      microservice.ConfigureApiPipelineInternal(action);

      service.PipelineMode = MicroServicePipelineMode.Api;

      return microservice;
    }

    /// <summary>
    /// Configures the default API pipeline, which uses Controllers, for the microservice
    /// </summary>
    /// <param name="microservice"></param>
    /// <returns><see cref="IMicroService"/></returns>
    public static IMicroService ConfigureApiControllerPipeline(this IMicroService microservice)
    {
      var service = (MicroService)microservice;

      microservice.ConfigureApiPipelineInternal((endpoints) =>
      {
        endpoints.MapControllers();
      });

      service.PipelineMode = MicroServicePipelineMode.ApiControllers;

      return microservice;
    }

    private static IMicroService ConfigureApiPipelineInternal(this IMicroService microservice, Action<IEndpointRouteBuilder> endpointBuilder)
    {
      var service = (MicroService)microservice;

      service.ValidatePipelineModeNotSet();

      service.ConfigureActions.Add(MicroService.Services.LifecycleServices);
      service.ConfigureActions.Add((svc, configuration) =>
      {
        svc.AddControllers();
        svc.AddEndpointsApiExplorer();
        svc.AddSwaggerGen(
          c =>
          {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = microservice.Name, Version = "v1" });
          });
      });

      service.UseCoreMicroServicePipeline(developmentOnlyPipeline: app =>
      {
        app.UseSwagger();
        app.UseSwaggerUI();
      });

      service
          .ConfigureExtensions()
          .ConfigurePipelineActions.Add(app =>
          {
            app.UseRouting();

            // Apply CORS middleware (uses default policy configured in Extension)
            var corsExtension = microservice.Extensions.SingleOrDefault(x => x.Is<CORS.Extension>());
            if (corsExtension is not null)
            {
              app.UseCors();
            }

            app.UseAuthorization();
            app.UseEndpoints(endpointBuilder);
          });

      return microservice;
    }
  }
}