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
    });

await service.RunAsync();