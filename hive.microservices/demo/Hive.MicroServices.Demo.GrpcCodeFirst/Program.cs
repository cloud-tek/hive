using Hive;
using Hive.MicroServices;
using Hive.MicroServices.Demo.GrpcCodeFirst.Services;
using Hive.MicroServices.Demo.WeatherForecasting;
using Hive.MicroServices.Grpc;
using Microsoft.Extensions.Logging.Abstractions;

var service = new MicroService("hive-microservices-grpc-code1st-demo", new NullLogger<IMicroService>())
    .ConfigureServices((services, configuration) =>
    {
      services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
    })
    .ConfigureCodeFirstGrpcPipeline(endpoints =>
    {
      endpoints.MapGrpcService<WeatherForecastingService>();
    });

await service.RunAsync();