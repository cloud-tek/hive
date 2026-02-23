using Hive.MicroServices;
using Hive.MicroServices.Api;
using Hive.MicroServices.Demo.ApiControllers.Client;
using Hive.MicroServices.Extensions;
using Hive.OpenTelemetry;

var service = new MicroService("hive-microservices-api-demo")
  .WithOpenTelemetry()
  .WithWeatherForecastApiClient()
  .ConfigureServices((services, _) =>
  {
    services.AddServiceDiscovery();
    services.ConfigureHttpClientDefaults(http => http.AddServiceDiscovery());
  })
  .ConfigureApiPipeline(app =>
  {
    app.MapGet(
      "/weatherforecast",
      async (IWeatherForecastApi api) => await api.GetWeatherForecast());
  });

await service.RunAsync();