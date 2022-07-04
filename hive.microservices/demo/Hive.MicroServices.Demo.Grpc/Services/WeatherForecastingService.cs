using Grpc.Core;
using Hive.MicroServices.Demo.WeatherForecasting;

namespace Hive.MicroServices.Demo.Grpc.Services
{
    public class WeatherForecastingService : WeatherForecasting.WeatherForecastingBase
    {
        private readonly IWeatherForecastService service;

        public WeatherForecastingService(IWeatherForecastService service)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public override Task<WeatherForecastResponse> GetWeatherForecast(WeatherForecastRequest request, ServerCallContext context)
        {
            var result = service.GetWeatherForecast();

            var response = new WeatherForecastResponse();

            response.Item.AddRange(result.Select(x => new WeatherForecastItem()
            {
                Date = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(x.Date),
                Summary = x.Summary,
                TemperatureC = x.TemperatureC,
                TemperatureF = x.TemperatureF
            }));

            return Task.FromResult(response);
        }
    }
}