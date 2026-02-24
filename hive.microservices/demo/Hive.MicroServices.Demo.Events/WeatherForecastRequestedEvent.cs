namespace Hive.MicroServices.Demo.Events;

public record WeatherForecastRequestedEvent(DateTime RequestedAt, int ForecastCount);
