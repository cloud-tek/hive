using Hive.Messaging.Configuration;
using Wolverine;

namespace Hive.Messaging.Serialization;

public sealed class SerializationBuilder
{
  private readonly WolverineOptions _options;
  internal MessagingSerialization Serialization { get; private set; } = MessagingSerialization.SystemTextJson;

  internal SerializationBuilder(WolverineOptions options)
  {
    _options = options;
  }

  public SerializationBuilder UseSystemTextJson()
  {
    Serialization = MessagingSerialization.SystemTextJson;
    return this;
  }
}
