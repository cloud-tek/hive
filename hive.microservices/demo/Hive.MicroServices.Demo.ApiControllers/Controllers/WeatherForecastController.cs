using Hive.MicroServices.Demo.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Hive.MicroServices.ApiControllers.Demo.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class WeatherForecastController : ControllerBase
  {
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IMessageBus _messageBus;

    public WeatherForecastController(
      ILogger<WeatherForecastController> logger,
      IMessageBus messageBus)
    {
      _logger = logger;
      _messageBus = messageBus;
    }

    [HttpGet]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
      _logger.LogInformation("Getting the forecast");
      var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
      {
        Date = DateTime.Now.AddDays(index),
        TemperatureC = Random.Shared.Next(-20, 55),
        Summary = Summaries[Random.Shared.Next(Summaries.Length)]
      })
      .ToArray();

      await _messageBus.PublishAsync(
        new WeatherForecastRequestedEvent(DateTime.UtcNow, forecasts.Length));

      return forecasts;
    }
  }
}