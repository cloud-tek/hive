using Hive.Messaging.RabbitMq.Configuration;

namespace Hive.Messaging.RabbitMq.Transport;

/// <summary>
/// Fluent builder for RabbitMQ transport configuration.
/// </summary>
public sealed class RabbitMqTransportBuilder
{
  internal RabbitMqOptions Options { get; } = new();

  internal RabbitMqTransportBuilder() { }

  public RabbitMqTransportBuilder ConnectionUri(string connectionUri)
  {
    Options.ConnectionUri = connectionUri;
    return this;
  }

  public RabbitMqTransportBuilder AutoProvision()
  {
    Options.AutoProvision = true;
    return this;
  }
}
