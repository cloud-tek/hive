#pragma warning disable CA2211, MA0069
using Hive.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Hive.MicroServices;

public partial class MicroService
{
  /// <summary>
  /// The default lifecycle middlewares for a microservice.
  /// </summary>
  public static class Middleware
  {
    /// <summary>
    /// The default lifecycle middlewares for a microservice.
    /// </summary>
    public static Action<IApplicationBuilder> MicroServiceLifecycleMiddlewares = (app) =>
      {
        app.UseMiddleware<StartupMiddleware>();
        app.UseMiddleware<ReadinessMiddleware>();
        app.UseMiddleware<ActiveRequestsMiddleware>();
      };
  }
}
#pragma warning restore CA2211, MA0069