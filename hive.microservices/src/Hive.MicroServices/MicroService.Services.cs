#pragma warning disable CA2211, MA0069
using Hive.MicroServices.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.MicroServices;

public partial class MicroService
{
  /// <summary>
  /// The default service collection for a microservice.
  /// </summary>
  public static class Services
  {
    /// <summary>
    /// The default lifecycle service registrations for a microservice.
    /// </summary>
    public static Action<IServiceCollection, IConfiguration> LifecycleServices = (svc, cfg) =>
      {
        svc.AddSingleton<IActiveRequestsService, ActiveRequestsService>();
        svc.AddHostedService<StartupService>();
        svc.AddHostedService<ShutdownService>();
      };
  }
}
#pragma warning restore CA2211, MA0069