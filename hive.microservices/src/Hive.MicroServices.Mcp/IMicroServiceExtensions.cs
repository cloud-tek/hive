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
  /// <remarks>
  /// Sets <see cref="MicroServicePipelineMode.Mcp"/> on the service. Pipeline mode is set once;
  /// calling a second <c>Configure*Pipeline</c> method on the same instance throws
  /// <see cref="InvalidOperationException"/> with message <c>"MicroService PipelineMode is already set"</c>.
  /// Auxiliary HTTP routes (control-plane endpoints, webhooks, admin actions) can be added alongside
  /// the MCP transport via <c>MapEndpoints(...)</c>, and they share this mode's routing, CORS, and
  /// authorization envelope.
  /// </remarks>
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
          endpoints.DrainCustomEndpoints(service);
        });
    });

    service.PipelineMode = MicroServicePipelineMode.Mcp;

    return microservice;
  }
}