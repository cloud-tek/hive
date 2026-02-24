using Hive.Messaging;
using Hive.Messaging.RabbitMq;
using Hive.MicroServices;
using Hive.MicroServices.Api;
using Hive.MicroServices.Demo.Events;
using Hive.OpenTelemetry;

var service = new MicroService("hive-microservices-apicontrollers-demo")
    .WithOpenTelemetry()
    .WithMessaging(builder => builder
      .UseRabbitMq()
      .WithSending(s => s
        .Publish<WeatherForecastRequestedEvent>()
        .ToQueue("q.demo.weatherforecastrequests")))
    .ConfigureApiControllerPipeline()
    ;

await service.RunAsync();
