using Hive.MicroServices.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.MicroServices.Mcp;

/// <summary>
/// Extension methods for <see cref="IMicroService"/>
/// </summary>
public static class IMicroServiceExtensions
{
  /// <summary>
  /// Configures a Model Context Protocol (MCP) server pipeline for the microservice, exposed over the streamable HTTP transport.
  /// </summary>
  /// <param name="microservice">The microservice to configure</param>
  /// <param name="serverBuilder">Callback used to register tools, prompts and resources on the MCP server</param>
  /// <returns><see cref="IMicroService"/></returns>
  public static IMicroService ConfigureMcpPipeline(this IMicroService microservice, Action<IMcpServerBuilder> serverBuilder)
  {
    _ = microservice ?? throw new ArgumentNullException(nameof(microservice));
    _ = serverBuilder ?? throw new ArgumentNullException(nameof(serverBuilder));

    var service = (MicroService)microservice;

    service.ValidatePipelineModeNotSet();

    service.ConfigureActions.Add(MicroService.Services.LifecycleServices);
    service.ConfigureActions.Add((svc, configuration) =>
    {
      var server = svc.AddMcpServer()
        .WithHttpTransport();

      serverBuilder(server);
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
          endpoints.MapMcp();
        });
    });

    service.PipelineMode = MicroServicePipelineMode.Mcp;

    return microservice;
  }
}