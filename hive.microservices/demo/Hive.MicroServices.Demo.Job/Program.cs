using Hive.MicroServices;
using Hive.MicroServices.Job;
using Hive.MicroServices.Job.Demo.Services;

var service = new MicroService("hive-microservices-job-demo")
    .ConfigureServices((services, configuration) =>
    {
      services
          .AddSingleton<IHostedJobService, JobService1>()
          .AddSingleton<IHostedJobService, JobService2>();
    })
    .ConfigureJob();

await service.RunAsync();