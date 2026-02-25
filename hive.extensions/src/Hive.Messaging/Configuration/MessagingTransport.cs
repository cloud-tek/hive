namespace Hive.Messaging.Configuration;

/// <summary>
/// Supported messaging transports.
/// </summary>
public enum MessagingTransport
{
  /// <summary>RabbitMQ message broker transport.</summary>
  RabbitMQ,

  /// <summary>In-memory transport (Wolverine built-in).</summary>
  InMemory
}
