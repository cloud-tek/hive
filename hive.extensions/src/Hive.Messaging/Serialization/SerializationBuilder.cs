using Hive.Messaging.Configuration;
using Wolverine;

namespace Hive.Messaging.Serialization;

/// <summary>
/// Fluent builder for configuring message serialization.
/// </summary>
public sealed class SerializationBuilder
{
  private readonly WolverineOptions _options;
  internal MessagingSerialization Serialization { get; private set; } = MessagingSerialization.SystemTextJson;

  internal SerializationBuilder(WolverineOptions options)
  {
    _options = options;
  }

  /// <summary>
  /// Configures the messaging system to use System.Text.Json for serialization.
  /// </summary>
  public SerializationBuilder UseSystemTextJson()
  {
    Serialization = MessagingSerialization.SystemTextJson;
    return this;
  }
}