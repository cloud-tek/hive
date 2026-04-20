using Hive.HTTP;

namespace Hive.MicroServices.Demo.ApiControllers.Client;

public static class Startup
{
  public static IMicroService WithWeatherForecastApiClient(this IMicroService service)
  {
    service.WithHttpClient<IWeatherForecastApi>(client => client.Internal());
    return service;
  }

  public static IMicroServiceCore WithWeatherForecastApiClient(this IMicroServiceCore service)
  {
    service.WithHttpClient<IWeatherForecastApi>(client => client.Internal());
    return service;
  }
}