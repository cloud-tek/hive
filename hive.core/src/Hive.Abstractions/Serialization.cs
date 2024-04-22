using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hive;

/// <summary>
/// Static class for controlling serialization options
/// </summary>
public static class Serialization
{
  /// <summary>
  /// JSon serialization options
  /// </summary>
  public static class JsonOptions
  {
#pragma warning disable CA221
    /// <summary>
    /// The default serialization options
    /// </summary>
    public static readonly JsonSerializerOptions DefaultIndented;
#pragma warning restore CA221

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