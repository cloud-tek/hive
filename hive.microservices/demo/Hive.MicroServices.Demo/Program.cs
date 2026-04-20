using Hive.HealthChecks;
using Hive.Messaging;
using Hive.Messaging.RabbitMq;
using Hive.Messaging.RabbitMq.HealthChecks;
using Hive.Microservices.Demo.Services;
using Hive.MicroServices.Demo.WeatherForecasting;
using Hive.MicroServices.Extensions;
using Hive.OpenTelemetry;

var service = new MicroService("hive-microservices-demo")
  .WithOpenTelemetry(additionalActivitySources: ["Hive.MicroServices.Demo"])
  .WithHealthChecks(checks => checks
    .WithHealthCheck<RabbitMqHealthCheck>())
  .WithMessaging(builder => builder
    .UseRabbitMq()
    .WithHandling(h => h
      .ListenToQueue("q.demo.weatherforecastrequests")))
  .ConfigureServices((services, configuration) =>
  {
    services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
    services.AddHostedService<WeatherForecastingService>();
  })
  .ConfigureDefaultServicePipeline();

await service.RunAsync();