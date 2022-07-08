using Hive;
using Hive.MicroServices;
using Hive.MicroServices.Demo.WeatherForecasting;
using Hive.MicroServices.GraphQL;
using Hive.MicroServices.GraphQL.Demo.Graph;
using Microsoft.Extensions.Logging.Abstractions;

var service = new MicroService("hive-microservices-graphql-demo", new NullLogger<IMicroService>())
    .ConfigureServices((services, configuration) =>
    {
        services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
    })
    .ConfigureGraphQLPipeline(schema =>
    {
        schema
              .AddQueryType<QueryType>();
    });

await service.RunAsync();
