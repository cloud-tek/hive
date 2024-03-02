using Hive.Extensions;
using HotChocolate.AspNetCore.Voyager;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.MicroServices.GraphQL;

/// <summary>
/// Extension methods for <see cref="IMicroService"/>
/// </summary>
public static class IMicroServiceExtensions
{
  /// <summary>
  /// Configures the default GraphQL pipeline for the microservice
  /// </summary>
  /// <param name="microservice"></param>
  /// <param name="schemaBuilder"></param>
  /// <returns><see cref="IMicroService"/></returns>
  public static IMicroService ConfigureGraphQLPipeline(this IMicroService microservice, Action<IRequestExecutorBuilder> schemaBuilder)
  {
    var service = (MicroService)microservice;

    service.ValidatePipelineModeNotSet();

    service.ConfigureActions.Add(MicroService.Services.LifecycleServices);
    service.ConfigureActions.Add((svc, configuration) =>
    {
      var server = svc.AddGraphQLServer();
      schemaBuilder(server);

      server.ModifyRequestOptions(opt => opt.IncludeExceptionDetails = service.Environment.IsDevelopment());
    });

    service.UseCoreMicroServicePipeline(developmentOnlyPipeline: app =>
    {
      app.UseVoyager(new VoyagerOptions()
      {
        Path = "/graphql-voyager",
        QueryPath = "/graphql"
      });
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
          endpoints.MapGraphQL("/graphql");
        });
    });

    service.PipelineMode = MicroServicePipelineMode.Api;

    return microservice;
  }
}