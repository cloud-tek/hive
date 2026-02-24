#pragma warning disable CA1848, CA2254
using System.Diagnostics;
using Hive.MicroServices.Demo.Events;
using Microsoft.Extensions.Logging;

namespace Hive.MicroServices.Demo.Handlers;

public class WeatherForecastRequestedHandler(
  ILogger<WeatherForecastRequestedHandler> logger)
{
  private static readonly ActivitySource Source = new("Hive.MicroServices.Demo");

  public async Task Handle(WeatherForecastRequestedEvent message)
  {
    using var activity = Source.StartActivity("ProcessWeatherForecastRequest");
    activity?.SetTag("forecast.count", message.ForecastCount);
    activity?.SetTag("forecast.requested_at", message.RequestedAt.ToString("O"));

    var delay = Random.Shared.Next(1, 5);
    logger.LogInformation(
      "Weather forecast requested at {RequestedAt} for {Count} forecasts, processing for {Delay}ms",
      message.RequestedAt, message.ForecastCount, delay);

    await Task.Delay(delay);

    activity?.SetTag("forecast.processing_ms", delay);
    logger.LogInformation(
      "Weather forecast completed for {Count} forecasts",
      message.ForecastCount);
  }
}
#pragma warning restore CA1848, CA2254
