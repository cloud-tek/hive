using Microsoft.Extensions.Logging;

namespace Hive.Functions.Demo.Services;

/// <summary>
/// Implementation of weather service
/// </summary>
public partial class WeatherService : IWeatherService
{
  private readonly ILogger<WeatherService> logger;
  private static readonly string[] Summaries = new[]
  {
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
  };

  /// <summary>
  /// Initializes a new instance of the WeatherService class
  /// </summary>
  /// <param name="logger">The logger</param>
  public WeatherService(ILogger<WeatherService> logger)
  {
    this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <inheritdoc />
  public Task<WeatherForecast> GetForecastAsync(string city)
  {
    LogGettingWeatherForecast(logger, city);

    var forecast = new WeatherForecast
    {
      City = city,
      Date = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
      TemperatureC = Random.Shared.Next(-20, 55),
      Summary = Summaries[Random.Shared.Next(Summaries.Length)]
    };

    return Task.FromResult(forecast);
  }

  /// <inheritdoc />
  public Task RefreshCacheAsync()
  {
    LogRefreshingWeatherCache(logger, DateTime.UtcNow);
    // In a real implementation, this would refresh cached weather data
    return Task.CompletedTask;
  }

  [LoggerMessage(LogLevel.Information, "Getting weather forecast for {City}")]
  private static partial void LogGettingWeatherForecast(ILogger logger, string city);

  [LoggerMessage(LogLevel.Information, "Refreshing weather cache at {Time}")]
  private static partial void LogRefreshingWeatherCache(ILogger logger, DateTime time);
}

/// <summary>
/// Weather forecast model
/// </summary>
public class WeatherForecast
{
  /// <summary>
  /// Gets or sets the city name
  /// </summary>
  public string City { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the forecast date
  /// </summary>
  public DateOnly Date { get; set; }

  /// <summary>
  /// Gets or sets the temperature in Celsius
  /// </summary>
  public int TemperatureC { get; set; }

  /// <summary>
  /// Gets the temperature in Fahrenheit
  /// </summary>
  public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

  /// <summary>
  /// Gets or sets the weather summary
  /// </summary>
  public string? Summary { get; set; }
}