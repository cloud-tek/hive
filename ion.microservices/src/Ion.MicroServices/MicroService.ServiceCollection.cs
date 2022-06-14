using Ion.MicroServices.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ion.MicroServices;

public partial class MicroService
{
    public static class ServiceCollection
    {
        public static Action<IServiceCollection, IConfiguration> LifecycleServices = (svc, cfg) =>
          {
              svc.AddSingleton<IActiveRequestsService, ActiveRequestsService>();
              svc.AddHostedService<StartupService>();
              svc.AddHostedService<ShutdownService>();
          };
    }
}