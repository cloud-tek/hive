using System.ServiceModel;
using ProtoBuf.Grpc;

namespace Hive.MicroServices.Demo.GrpcCodeFirst.Services;

[ServiceContract]
public interface IWeatherForecastingService
{
  [OperationContract]
  Task<WeatherForecastResponse[]> GetWeatherForecast(CallContext context = default);
}