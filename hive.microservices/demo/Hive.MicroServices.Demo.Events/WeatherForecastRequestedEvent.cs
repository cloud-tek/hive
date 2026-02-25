namespace Hive.MicroServices.Demo.Events;

/// <summary>
/// Event published when a weather forecast is requested.
/// </summary>
/// <param name="RequestedAt">The time the forecast was requested.</param>
/// <param name="ForecastCount">The number of forecasts requested.</param>
public record WeatherForecastRequestedEvent(DateTime RequestedAt, int ForecastCount);
