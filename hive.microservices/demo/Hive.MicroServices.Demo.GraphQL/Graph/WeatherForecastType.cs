
#pragma warning disable CS1591
using Hive.MicroServices.Demo.WeatherForecasting;
using HotChocolate.Types;

namespace Hive.MicroServices.GraphQL.Demo.Graph;

public class WeatherForecastType : ObjectType<WeatherForecast>
{
  protected override void Configure(IObjectTypeDescriptor<WeatherForecast> descriptor)
  {
    descriptor
        .Field(f => f.Summary)
        .Type<StringType>();

    descriptor
        .Field(f => f.Date)
        .Type<DateTimeType>();

    descriptor
        .Field(f => f.TemperatureC)
        .Type<IntType>();

    descriptor
        .Field(f => f.TemperatureF)
        .Type<IntType>();
  }
}
#pragma warning restore CS1591