using HotChocolate.Types;
using Hive.MicroServices.Demo.WeatherForecasting;

namespace Hive.MicroServices.GraphQL.Demo.Graph;

public class Query
{
}

public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field("weatherForecast")
            .Type<ListType<WeatherForecastType>>()
            .Resolve(ctx => ctx.Service<IWeatherForecastService>().GetWeatherForecast());
    }
}