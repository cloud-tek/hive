using Hive.MicroServices;
using Hive.MicroServices.Api;
using Hive.OpenTelemetry;

var service = new MicroService("hive-microservices-apicontrollers-demo")
    .WithOpenTelemetry()
    .ConfigureApiControllerPipeline()
    ;

await service.RunAsync();