namespace Hive.MicroServices.Demo.WeatherForecasting;

public interface IWeatherForecastService
{
    IEnumerable<WeatherForecast> GetWeatherForecast();
}