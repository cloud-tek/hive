namespace Hive.Messaging.RabbitMq.Configuration;

/// <summary>
/// RabbitMQ transport configuration options.
/// </summary>
public class RabbitMqOptions
{
  public string? ConnectionUri { get; set; }
  public bool AutoProvision { get; set; }
}
