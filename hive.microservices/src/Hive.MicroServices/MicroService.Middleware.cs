using Hive.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Hive.MicroServices;

public partial class MicroService
{
    public static class Middleware
    {
        public static Action<IApplicationBuilder> MicroServiceLifecycleMiddlewares = (app) =>
          {
              app.UseMiddleware<StartupMiddleware>();
              app.UseMiddleware<ReadinessMiddleware>();
              app.UseMiddleware<ActiveRequestsMiddleware>();
          };
    }
}
