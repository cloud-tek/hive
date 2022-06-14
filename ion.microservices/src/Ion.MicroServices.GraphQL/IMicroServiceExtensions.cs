using HotChocolate.AspNetCore.Voyager;
using HotChocolate.Execution.Configuration;
using Ion.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Ion.MicroServices.GraphQL;

public static class IMicroServiceExtensions
{
    public static IMicroService ConfigureGraphQLPipeline(this IMicroService microservice, Action<IRequestExecutorBuilder> schemaBuilder)
    {
        var service = (MicroService)microservice;

        service.ValidatePipelineModeNotSet();

        service.ConfigureActions.Add(MicroService.ServiceCollection.LifecycleServices);
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