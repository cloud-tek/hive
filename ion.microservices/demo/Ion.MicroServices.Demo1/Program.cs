using Ion.Microservices.Demo.Services;
using Ion.MicroServices;
using Ion.Logging;
using Ion.MicroServices.Demo.WeatherForecasting;

var service = new MicroService("ion-microservices-demo")
    .WithLogging(log =>
    {
        log.ToConsole();
    })
    .ConfigureServices((services, configuration) =>
    {
        services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
        services.AddHostedService<WeatherForecastingService>();
    })
    .ConfigureDefaultServicePipeline();

await service.RunAsync();