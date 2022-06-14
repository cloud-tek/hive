using Ion.Logging;
using Ion.Logging.LogzIo;
using Ion.MicroServices;
using Ion.MicroServices.Api;

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

var service = new MicroService("ion-microservices-api-demo")
    .WithLogging(log =>
    {
        log
            .ToConsole()
            .ToLogzIo();
    })
    .ConfigureServices((services, _) => { })
    .ConfigureApiPipeline(app =>
    {
        app.MapGet("/weatherforecast", () =>
        {
            var forecast = Enumerable.Range(1, 5).Select(index =>
               new WeatherForecast
               (
                   DateTime.Now.AddDays(index),
                   Random.Shared.Next(-20, 55),
                   summaries[Random.Shared.Next(summaries.Length)]
               ))
                .ToArray();
            return forecast;
        });
    });

await service.RunAsync();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}