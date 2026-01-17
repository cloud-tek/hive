using System.Net;
using Hive.Functions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Hive.Functions.Demo.Functions;

/// <summary>
/// Weather-related Azure Functions
/// </summary>
public class WeatherFunction
{
  private readonly IWeatherService weatherService;
  private readonly ILogger<WeatherFunction> logger;

  /// <summary>
  /// Initializes a new instance of the WeatherFunction class
  /// </summary>
  /// <param name="weatherService">The weather service</param>
  /// <param name="logger">The logger</param>
  public WeatherFunction(
    IWeatherService weatherService,
    ILogger<WeatherFunction> logger)
  {
    this.weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
    this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <summary>
  /// HTTP trigger function to get weather forecast for a city
  /// </summary>
  [Function("GetWeather")]
  public async Task<HttpResponseData> GetWeather(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "weather/{city}")] HttpRequestData req,
    string city,
    FunctionContext context)
  {
    logger.LogInformation("Processing weather request for {City}", city);

    try
    {
      var forecast = await weatherService.GetForecastAsync(city);

      var response = req.CreateResponse(HttpStatusCode.OK);
      await response.WriteAsJsonAsync(forecast);
      return response;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error retrieving weather for {City}", city);

      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteAsJsonAsync(new { error = "Failed to retrieve weather forecast" });
      return errorResponse;
    }
  }

  /// <summary>
  /// Timer trigger function to refresh weather cache every 5 minutes
  /// </summary>
  [Function("WeatherCacheRefresh")]
  public async Task WeatherCacheRefresh(
    [TimerTrigger("0 */5 * * * *")] TimerInfo timer,
    FunctionContext context)
  {
    logger.LogInformation("Weather cache refresh timer trigger executed at {Time}", DateTime.UtcNow);
    logger.LogInformation("Next timer schedule at {NextSchedule}", timer.ScheduleStatus?.Next);

    try
    {
      await weatherService.RefreshCacheAsync();
      logger.LogInformation("Weather cache refreshed successfully");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error refreshing weather cache");
      throw;
    }
  }
}