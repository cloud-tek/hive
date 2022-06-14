using Ion;
using Ion.MicroServices;
using Ion.MicroServices.Demo.WeatherForecasting;
using Ion.MicroServices.GraphQL;
using Ion.MicroServices.GraphQL.Demo.Graph;
using Microsoft.Extensions.Logging.Abstractions;

var service = new MicroService("ion-microservices-graphql-demo", new NullLogger<IMicroService>())
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