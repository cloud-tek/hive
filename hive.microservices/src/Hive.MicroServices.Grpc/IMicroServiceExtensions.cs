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
            var corsExtension = microservice.Extensions.SingleOrDefault(x => x.Is<CORS.Extension>());
            if (corsExtension is not null)
            {
              app.UseCors();
            }

            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
              {
                endpointsBuilder(endpoints);
                endpoints.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
              });
          });

      service.PipelineMode = MicroServicePipelineMode.Grpc;

      return microservice;
    }
  }
}