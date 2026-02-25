using Hive.Messaging.RabbitMq.Configuration;

namespace Hive.Messaging.RabbitMq.Transport;

/// <summary>
/// Fluent builder for RabbitMQ transport configuration.
/// </summary>
public sealed class RabbitMqTransportBuilder
{
  internal RabbitMqOptions Options { get; } = new();

  internal RabbitMqTransportBuilder() { }

  /// <summary>
  /// Sets the AMQP connection URI for the RabbitMQ broker.
  /// </summary>
  /// <param name="connectionUri">The AMQP connection URI.</param>
  public RabbitMqTransportBuilder ConnectionUri(string connectionUri)
  {
    Options.ConnectionUri = connectionUri;
    return this;
  }

  /// <summary>
  /// Enables auto-provisioning of queues and exchanges on the broker.
  /// </summary>
  public RabbitMqTransportBuilder AutoProvision()
  {
    Options.AutoProvision = true;
    return this;
  }
}