namespace Hive.Messaging.Configuration;

/// <summary>
/// Configuration options for Hive.Messaging. Transport-specific options are bound
/// by the transport provider from IConfiguration.
/// </summary>
public class MessagingOptions
{
  public const string SectionKey = "Hive:Messaging";

  public MessagingTransport Transport { get; set; } = MessagingTransport.InMemory;
  public MessagingSerialization Serialization { get; set; } = MessagingSerialization.SystemTextJson;
  public Dictionary<string, NamedBrokerOptions> NamedBrokers { get; set; } = new();
  public HandlingOptions Handling { get; set; } = new();
}

/// <summary>
/// Named broker options. Transport-specific config is read from IConfiguration by the provider.
/// </summary>
public class NamedBrokerOptions { }
