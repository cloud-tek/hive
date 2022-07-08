using System.Runtime.Serialization;

namespace Hive.MicroServices.Demo.GrpcCodeFirst.Services
{
    [DataContract]
    public class WeatherForecastResponse
    {
        [DataMember(Order = 1)]
        public DateTime Date { get; set; }

        [DataMember(Order = 2)]
        public int TemperatureC { get; set; }

        [DataMember(Order = 3)]
        public int TemperatureF { get; set; }

        [DataMember(Order = 4)]
        public string Summary { get; set; }
    }
}