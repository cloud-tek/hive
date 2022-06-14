using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Ion.MicroServices.Api
{
    public static class IMicroServiceExtensions
    {
        public static IMicroService ConfigureApiPipeline(this IMicroService microservice, Action<Microsoft.AspNetCore.Routing.IEndpointRouteBuilder> action)
        {
            var service = (MicroService)microservice;

            microservice.ConfigureApiPipelineInternal(action);

            service.PipelineMode = MicroServicePipelineMode.Api;

            return microservice;
        }

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

            service.ConfigureActions.Add(MicroService.ServiceCollection.LifecycleServices);
            service.ConfigureActions.Add((svc, configuration) =>
            {
                svc.AddControllers();
                svc.AddEndpointsApiExplorer();
                svc.AddSwaggerGen(c =>
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
                    app.UseAuthorization();
                    app.UseEndpoints(endpointBuilder);
                });

            return microservice;
        }
    }
}