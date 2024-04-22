using Hive;
using Hive.MicroServices;
using Hive.MicroServices.Demo.Grpc.Services;
using Hive.MicroServices.Demo.WeatherForecasting;
using Hive.MicroServices.Grpc;
using Microsoft.Extensions.Logging.Abstractions;

var service = new MicroService("hive-microservices-grpc-demo", new NullLogger<IMicroService>())
    .ConfigureServices((services, configuration) =>
    {
      services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
    })
    .ConfigureGrpcPipeline(endpoints =>
    {
      endpoints.MapGrpcService<WeatherForecastingService>();
    });

await service.RunAsync();