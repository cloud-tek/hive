using Ion;
using Ion.MicroServices;
using Ion.MicroServices.Demo.Grpc.Services;
using Ion.MicroServices.Demo.WeatherForecasting;
using Ion.MicroServices.Grpc;
using Microsoft.Extensions.Logging.Abstractions;

var service = new MicroService("ion-microservices-grpc-demo", new NullLogger<IMicroService>())
    .ConfigureServices((services, configuration) =>
    {
        services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
    })
    .ConfigureGrpcPipeline(endpoints =>
    {
        endpoints.MapGrpcService<WeatherForecastingService>();
    });

await service.RunAsync();