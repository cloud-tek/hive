using Hive.MicroServices.Job.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.MicroServices.Job
{
  /// <summary>
  /// Extension methods for <see cref="IMicroService"/>
  /// </summary>
  public static class IMicroServiceExtensions
  {
    /// <summary>
    /// Configures a JOB pipeline for the microservice
    /// </summary>
    /// <param name="microservice"></param>
    /// <returns><see cref="IMicroService"/></returns>
    /// <exception cref="ArgumentNullException">Thrown when any of the provided arguments are null</exception>
    public static IMicroService ConfigureJob(this IMicroService microservice)
    {
      _ = microservice ?? throw new ArgumentNullException(nameof(microservice));

      var service = (MicroService)microservice;

      service.ValidatePipelineModeNotSet();

      service.ConfigureActions.Add(MicroService.Services.LifecycleServices);
      service.ConfigureActions.Add((svc, configuration) =>
      {
        svc.AddHostedService<JobHostedService>();
      });

      service
          .ConfigureExtensions()
          .ConfigurePipelineActions.Add(app =>
          {
            app.UseRouting();
            app.When(
              () => microservice.Extensions.Any(x => x.Is<CORS.Extension>()),
              (a) =>
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