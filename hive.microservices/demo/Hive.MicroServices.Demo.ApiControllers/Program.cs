using Hive.MicroServices;
using Hive.MicroServices.Api;
using Hive.MicroServices.Extensions;

var service = new MicroService("hive-microservices-apicontrollers-demo")
    .ConfigureServices((services, configuration) => { })
    .ConfigureApiControllerPipeline()
    ;

await service.RunAsync();