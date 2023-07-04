using Hive.MicroServices.CORS;

namespace Hive.MicroServices;

public static class MicroServiceExtensions
{
  public static IMicroService WithCors(this IMicroService service,params string[] urls)
  {
    service.Extensions.Add(new Extension(service));

    return service;
  }
}
