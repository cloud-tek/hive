using Hive.Microservices.Demo.Services;
using Hive.MicroServices.Demo.WeatherForecasting;

var service = new MicroService("hive-microservices-demo")
  .WithCORS()
  .ConfigureServices((services, configuration) =>
  {
    services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
    services.AddHostedService<WeatherForecastingService>();
  })
  .ConfigureDefaultServicePipeline();

await service.RunAsync();