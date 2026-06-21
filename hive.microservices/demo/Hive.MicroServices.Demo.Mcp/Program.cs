using Hive.MicroServices;
using Hive.MicroServices.Demo.WeatherForecasting;
using Hive.MicroServices.Extensions;
using Hive.MicroServices.Mcp;
using Hive.MicroServices.Mcp.Demo.Tools;
using Hive.OpenTelemetry;

var service = new MicroService("hive-microservices-mcp-demo")
    .WithOpenTelemetry()
    .ConfigureServices((services, configuration) =>
    {
      services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
    })
    .ConfigureMcpPipeline(mcp =>
    {
      mcp.WithTools<WeatherForecastTool>();
    })
    .MapEndpoints(routes =>
    {
      // Custom admin endpoint sharing the same DI singleton as WeatherForecastTool.
      // Demonstrates that MapEndpoints routes run inside the same routing/auth envelope.
      routes.MapGet("/admin/forecast/summary", (IWeatherForecastService svc) =>
        Results.Ok(new { count = svc.GetWeatherForecast().Count() }));
    });

await service.RunAsync();