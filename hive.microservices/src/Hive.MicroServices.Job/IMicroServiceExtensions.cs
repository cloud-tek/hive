using Hive.MicroServices.Job.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.MicroServices.Job
{
    public static class IMicroServiceExtensions
    {
        public static IMicroService ConfigureJob(this IMicroService microservice)
        {
            if (microservice == null) throw new ArgumentNullException(nameof(microservice));

            var service = (MicroService)microservice;

            service.ValidatePipelineModeNotSet();

            service.ConfigureActions.Add(MicroService.ServiceCollection.LifecycleServices);
            service.ConfigureActions.Add((svc, configuration) =>
            {
                svc.AddHostedService<JobHostedService>();
            });

            service
                .ConfigureExtensions()
                .ConfigurePipelineActions.Add(app =>
                {
                    app.UseRouting();
                    app.When(() => microservice.Extensions.Any(x => x.Is<CORS.Extension>()), (a) =>
                    {
                      a.UseCors();
                    });
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/", () => "Communication with a Hive.MicroServices.Job is not possible. The service will execute all IHostedService(s) and shut down");
                    });
                });

            service.PipelineMode = MicroServicePipelineMode.None;

            return microservice;
        }
    }
}
