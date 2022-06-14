using ProtoBuf.Grpc;
using System.ServiceModel;

namespace Ion.MicroServices.Demo.GrpcCodeFirst.Services;

[ServiceContract]
public interface IWeatherForecastingService
{
    [OperationContract]
    Task<WeatherForecastResponse[]> GetWeatherForecast(CallContext context = default);
}