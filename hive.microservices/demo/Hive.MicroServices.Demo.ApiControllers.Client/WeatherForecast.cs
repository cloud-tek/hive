namespace Hive.MicroServices.Demo.ApiControllers.Client;

public sealed record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
  public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}