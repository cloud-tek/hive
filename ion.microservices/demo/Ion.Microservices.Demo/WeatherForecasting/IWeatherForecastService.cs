namespace Ion.MicroServices.Demo.WeatherForecasting;

public interface IWeatherForecastService
{
    IEnumerable<WeatherForecast> GetWeatherForecast();
}