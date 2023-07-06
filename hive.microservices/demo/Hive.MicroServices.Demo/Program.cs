using Hive.Microservices.Demo.Services;
using Hive.MicroServices;
using Hive.Logging;
using Hive.MicroServices.Demo.WeatherForecasting;

var service = new MicroService("hive-microservices-demo")
    .WithLogging(log =>
    {
        log.ToConsole();
    })
    .WithCORS()
    .ConfigureServices((services, configuration) =>
    {
        services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
        services.AddHostedService<WeatherForecastingService>();
    })
    .ConfigureDefaultServicePipeline();

await service.RunAsync();
