using Hive.Logging;
using Hive.MicroServices;
using Hive.MicroServices.Api;
using Hive.MicroServices.Demo.Api;

var summaries = new[]
{
  "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

var service = new MicroService("hive-microservices-api-demo")
  .WithCORS()
  .WithLogging(
    log =>
    {
      log
        .ToConsole();
    })
  .ConfigureServices((services, _) => { })
  .ConfigureApiPipeline(
    app =>
    {
      app.MapGet(
        "/weatherforecast",
        () =>
        {
          var forecast = Enumerable.Range(1, 5).Select(
              index =>
                new WeatherForecast(
                  DateTime.Now.AddDays(index),
                  Random.Shared.Next(-20, 55),
                  summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
          return forecast;
        });
    });

await service.RunAsync();