using Ion.MicroServices;
using Ion.MicroServices.Job;
using Ion.MicroServices.Job.Demo.Services;

var service = new MicroService("ion-microservices-job-demo")
    .ConfigureServices((services, configuration) =>
    {
        services
            .AddSingleton<IHostedJobService, JobService1>()
            .AddSingleton<IHostedJobService, JobService2>();
    })
    .ConfigureJob();

await service.RunAsync();