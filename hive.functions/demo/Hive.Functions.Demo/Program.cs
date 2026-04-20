using Hive.Functions;
using Hive.Functions.Demo.Services;
using Microsoft.Extensions.DependencyInjection;

var functionHost = new FunctionHost("hive-functions-demo")
  .WithOpenTelemetry()
  .ConfigureServices((services, config) =>
  {
    // Register application services
    services.AddSingleton<IWeatherService, WeatherService>();
    services.AddHttpClient();
  });

await functionHost.RunAsync();