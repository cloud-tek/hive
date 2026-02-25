namespace Hive.Messaging.Configuration;

/// <summary>
/// Supported serialization formats for message payloads.
/// </summary>
public enum MessagingSerialization
{
  /// <summary>System.Text.Json serialization.</summary>
  SystemTextJson,

  /// <summary>Newtonsoft.Json serialization.</summary>
  NewtonsoftJson,

  /// <summary>MessagePack binary serialization.</summary>
  MessagePack,

  /// <summary>Protocol Buffers binary serialization.</summary>
  Protobuf
}
