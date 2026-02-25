namespace Hive.Messaging.RabbitMq.Configuration;

/// <summary>
/// RabbitMQ transport configuration options.
/// </summary>
public class RabbitMqOptions
{
  /// <summary>
  /// The AMQP connection URI for the RabbitMQ broker.
  /// </summary>
  public string? ConnectionUri { get; set; }

  /// <summary>
  /// When true, Wolverine will auto-provision queues and exchanges on the broker.
  /// </summary>
  public bool AutoProvision { get; set; }
}