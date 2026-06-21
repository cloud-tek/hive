using Hive.Exceptions;
using Hive.MicroServices.Extensions;
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
    /// <remarks>
    /// Sets <see cref="MicroServicePipelineMode.None"/> on the service (worker/job intent). Pipeline mode
    /// is set once; calling a second <c>Configure*Pipeline</c> method on the same instance throws
    /// <see cref="InvalidOperationException"/> with message <c>"MicroService PipelineMode is already set"</c>.
    /// Job (worker) services cannot serve custom HTTP routes: if <c>MapEndpoints(...)</c> has been called on
    /// the service, the pipeline throws <see cref="Hive.Exceptions.ConfigurationException"/> at startup with
    /// message <c>"Hive.MicroServices.Job (worker) services cannot expose custom HTTP endpoints via MapEndpoints.
    /// Remove the MapEndpoints call, or select an HTTP pipeline mode (e.g. ConfigureApiPipeline /
    /// ConfigureGraphQLPipeline)."</c>
    /// </remarks>
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
            if (service.MapEndpointActions.Count > 0)
            {
              throw new ConfigurationException(Constants.Errors.MapEndpointsJobForbidden);
            }

            app.UseRouting();

            // Apply CORS middleware (uses default policy configured in Extension)
            var corsExtension = microservice.Extensions.SingleOrDefault(x => x is CORS.Extension);
            if (corsExtension is not null)
            {
              app.UseCors();
            }

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