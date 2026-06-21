using Hive.MicroServices.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Server;

namespace Hive.MicroServices.Grpc
{
  /// <summary>
  /// Extension methods for <see cref="IMicroService"/>
  /// </summary>
  public static class IMicroServiceExtensions
  {
    /// <summary>
    /// Configures a gRPC pipeline for a microservice
    /// </summary>
    /// <param name="microservice"></param>
    /// <param name="endpointsBuilder"></param>
    /// <returns><see cref="IMicroService"/></returns>
    /// <remarks>
    /// Sets <see cref="MicroServicePipelineMode.Grpc"/> on the service. Pipeline mode is set once;
    /// calling a second <c>Configure*Pipeline</c> method on the same instance throws
    /// <see cref="InvalidOperationException"/> with message <c>"MicroService PipelineMode is already set"</c>.
    /// Auxiliary HTTP routes (control-plane endpoints, webhooks, admin actions) can be added alongside
    /// the gRPC services via <c>MapEndpoints(...)</c>; they share this mode's routing, CORS, and
    /// authorization envelope, and are registered before the mode's catch-all fallback route.
    /// </remarks>
    public static IMicroService ConfigureGrpcPipeline(this IMicroService microservice, Action<IEndpointRouteBuilder> endpointsBuilder)
    {
      return microservice.ConfigureGrpcPipelineInternal(endpointsBuilder, configureGrpc: (services) => services.AddGrpc());
    }

    /// <summary>
    /// Configures a gRPC pipeline for a microservice, using CodeFirst
    /// </summary>
    /// <param name="microservice"></param>
    /// <param name="endpointsBuilder"></param>
    /// <returns><see cref="IMicroService"/></returns>
    /// <remarks>
    /// Sets <see cref="MicroServicePipelineMode.Grpc"/> on the service using the protobuf-net code-first
    /// gRPC variant. Pipeline mode is set once; calling a second <c>Configure*Pipeline</c> method on the
    /// same instance throws <see cref="InvalidOperationException"/> with message
    /// <c>"MicroService PipelineMode is already set"</c>. Auxiliary HTTP routes can be added alongside
    /// the gRPC services via <c>MapEndpoints(...)</c>, sharing this mode's routing, CORS, and
    /// authorization envelope.
    /// </remarks>
    public static IMicroService ConfigureCodeFirstGrpcPipeline(this IMicroService microservice, Action<IEndpointRouteBuilder> endpointsBuilder)
    {
      return microservice.ConfigureGrpcPipelineInternal(endpointsBuilder, configureGrpc: (services) => services.AddCodeFirstGrpc());
    }

    private static IMicroService ConfigureGrpcPipelineInternal(this IMicroService microservice, Action<IEndpointRouteBuilder> endpointsBuilder, Action<IServiceCollection> configureGrpc)
    {
      _ = microservice ?? throw new ArgumentNullException(nameof(microservice));
      _ = endpointsBuilder ?? throw new ArgumentNullException(nameof(endpointsBuilder));
      _ = configureGrpc ?? throw new ArgumentNullException(nameof(configureGrpc));

      var service = (MicroService)microservice;

      service.ValidatePipelineModeNotSet();

      service.ConfigureActions.Add(MicroService.Services.LifecycleServices);

      service.ConfigureActions.Add((services, configuration) =>
      {
        configureGrpc(services);
      });

      service.UseCoreMicroServicePipeline();

      service
          .ConfigureExtensions()
          .ConfigurePipelineActions.Add(app =>
          {
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
                endpointsBuilder(endpoints);
                endpoints.DrainCustomEndpoints(service);
                endpoints.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
              });
          });

      service.PipelineMode = MicroServicePipelineMode.Grpc;

      return microservice;
    }
  }
}