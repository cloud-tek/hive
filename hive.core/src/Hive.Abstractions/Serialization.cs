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
    private static readonly JsonSerializerOptions _defaultIndented = CreateDefaultIndented();

    /// <summary>
    /// The default serialization options.
    /// </summary>
    /// <remarks>
    /// This instance is frozen via <see cref="JsonSerializerOptions.MakeReadOnly()"/> and is safe to share across threads.
    /// Any attempt to mutate properties or converters will throw <see cref="InvalidOperationException"/>.
    /// </remarks>
    public static JsonSerializerOptions DefaultIndented => _defaultIndented;

    private static JsonSerializerOptions CreateDefaultIndented()
    {
      var options = new JsonSerializerOptions()
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
      };
      options.Converters.Add(new JsonStringEnumConverter());
      options.MakeReadOnly();
      return options;
    }
  }
}