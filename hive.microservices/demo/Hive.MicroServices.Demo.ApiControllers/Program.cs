using Hive.Logging;
using Hive.Logging.AppInsights;
using Hive.Logging.LogzIo;
using Hive.MicroServices;
using Hive.MicroServices.Api;

var service = new MicroService("hive-microservices-apicontrollers-demo")
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
