using Refit;

namespace Hive.MicroServices.Demo.ApiControllers.Client;

[Headers("Accept: application/json")]
public interface IWeatherForecastApi
{
  [Get("/weatherforecast")]
  Task<WeatherForecast[]> GetWeatherForecast();
}