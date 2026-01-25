namespace Hive.Functions.Demo.Services;

/// <summary>
/// Service for retrieving weather information
/// </summary>
public interface IWeatherService
{
  /// <summary>
  /// Gets the weather forecast for a city
  /// </summary>
  /// <param name="city">The city name</param>
  /// <returns>Weather forecast information</returns>
  Task<WeatherForecast> GetForecastAsync(string city);

  /// <summary>
  /// Refreshes the weather cache
  /// </summary>
  Task RefreshCacheAsync();
}