using System.ComponentModel;
using Hive.MicroServices.Demo.WeatherForecasting;
using ModelContextProtocol.Server;

namespace Hive.MicroServices.Mcp.Demo.Tools;

/// <summary>
/// A sample MCP tool exposing the demo weather forecast over the Model Context Protocol.
/// </summary>
[McpServerToolType]
public class WeatherForecastTool
{
  /// <summary>
  /// Returns a short-range weather forecast.
  /// </summary>
  /// <param name="service">The weather forecast service, resolved from DI</param>
  /// <returns>The forecast for the next few days</returns>
  [McpServerTool(Name = "get_weather_forecast")]
  [Description("Gets the weather forecast for the next few days.")]
  public static IEnumerable<WeatherForecast> GetWeatherForecast(IWeatherForecastService service)
  {
    return service.GetWeatherForecast();
  }
}