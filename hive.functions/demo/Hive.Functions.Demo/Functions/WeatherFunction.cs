using System.Net;
using Hive.Functions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Hive.Functions.Demo.Functions;

/// <summary>
/// Weather-related Azure Functions
/// </summary>
public partial class WeatherFunction
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
    LogProcessingWeatherRequest(logger, city);

    try
    {
      var forecast = await weatherService.GetForecastAsync(city);

      var response = req.CreateResponse(HttpStatusCode.OK);
      await response.WriteAsJsonAsync(forecast);
      return response;
    }
    catch (Exception ex)
    {
      LogErrorRetrievingWeather(logger, city, ex);

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
    LogWeatherCacheRefreshTrigger(logger, DateTime.UtcNow);
    LogNextTimerSchedule(logger, timer.ScheduleStatus?.Next);

    try
    {
      await weatherService.RefreshCacheAsync();
      LogWeatherCacheRefreshed(logger);
    }
    catch (Exception ex)
    {
      LogErrorRefreshingCache(logger, ex);
      throw;
    }
  }

  [LoggerMessage(LogLevel.Information, "Processing weather request for {City}")]
  private static partial void LogProcessingWeatherRequest(ILogger logger, string city);

  [LoggerMessage(LogLevel.Error, "Error retrieving weather for {City}")]
  private static partial void LogErrorRetrievingWeather(ILogger logger, string city, Exception ex);

  [LoggerMessage(LogLevel.Information, "Weather cache refresh timer trigger executed at {Time}")]
  private static partial void LogWeatherCacheRefreshTrigger(ILogger logger, DateTime time);

  [LoggerMessage(LogLevel.Information, "Next timer schedule at {NextSchedule}")]
  private static partial void LogNextTimerSchedule(ILogger logger, DateTimeOffset? nextSchedule);

  [LoggerMessage(LogLevel.Information, "Weather cache refreshed successfully")]
  private static partial void LogWeatherCacheRefreshed(ILogger logger);

  [LoggerMessage(LogLevel.Error, "Error refreshing weather cache")]
  private static partial void LogErrorRefreshingCache(ILogger logger, Exception ex);
}