using Hive.Microservices.Demo.Services;
using Hive.MicroServices.Demo.WeatherForecasting;
using Hive.MicroServices.Extensions;

var service = new MicroService("hive-microservices-demo")
  .ConfigureServices((services, configuration) =>
  {
    services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
    services.AddHostedService<WeatherForecastingService>();
  })
  .ConfigureDefaultServicePipeline();

await service.RunAsync();