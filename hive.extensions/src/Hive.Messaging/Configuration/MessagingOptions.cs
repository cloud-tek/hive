namespace Hive.Messaging.Configuration;

/// <summary>
/// Configuration options for Hive.Messaging. Transport-specific options are bound
/// by the transport provider from IConfiguration.
/// </summary>
public class MessagingOptions
{
  /// <summary>
  /// The configuration section key used to bind messaging options.
  /// </summary>
  public const string SectionKey = "Hive:Messaging";

  /// <summary>
  /// The messaging transport to use (e.g. RabbitMQ, InMemory).
  /// </summary>
  public MessagingTransport Transport { get; set; } = MessagingTransport.InMemory;

  /// <summary>
  /// The serialization format for message payloads.
  /// </summary>
  public MessagingSerialization Serialization { get; set; } = MessagingSerialization.SystemTextJson;

  /// <summary>
  /// Named broker configurations for multi-broker topologies.
  /// </summary>
  public Dictionary<string, NamedBrokerOptions> NamedBrokers { get; set; } = new();

  /// <summary>
  /// Options controlling message handler behavior.
  /// </summary>
  public HandlingOptions Handling { get; set; } = new();
}

/// <summary>
/// Named broker options. Transport-specific config is read from IConfiguration by the provider.
/// </summary>
public class NamedBrokerOptions { }