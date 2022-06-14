using Ion.Logging;
using Ion.Logging.AppInsights;
using Ion.Logging.LogzIo;
using Ion.MicroServices;
using Ion.MicroServices.Api;

var service = new MicroService("ion-microservices-apicontrollers-demo")
        .WithLogging(log =>
        {
            log
                .ToConsole()
                .ToLogzIo()
                .ToAppInsights();
        })
    .ConfigureServices((services, configuration) => { })
    .ConfigureApiControllerPipeline()
    ;

await service.RunAsync();