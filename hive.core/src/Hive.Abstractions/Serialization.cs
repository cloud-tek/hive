using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hive;

public static class Serialization
{
    public static class JsonOptions
    {
        public static JsonSerializerOptions DefaultIndented;

        static JsonOptions()
        {
            DefaultIndented = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
            DefaultIndented.Converters.Add(new JsonStringEnumConverter());
        }
    }
}
