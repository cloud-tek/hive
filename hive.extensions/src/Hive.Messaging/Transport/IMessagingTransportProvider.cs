using Hive.Messaging.Configuration;
using Microsoft.Extensions.Configuration;
using Wolverine;

namespace Hive.Messaging.Transport;

/// <summary>
/// Provides transport-specific Wolverine configuration, routing, and validation.
/// Implemented by transport packages (e.g., Hive.Messaging.RabbitMq).
/// </summary>
public interface IMessagingTransportProvider
{
  /// <summary>
  /// Apply the primary transport configuration to Wolverine options.
  /// The provider should bind its own options from IConfiguration.
  /// </summary>
  void ConfigureTransport(WolverineOptions opts, MessagingOptions options, IConfiguration configuration);

  /// <summary>
  /// Register a queue listener for this transport.
  /// </summary>
  void ListenToQueue(WolverineOptions opts, string queueName, string? brokerName,
    int? prefetchCount, int? listenerCount);

  /// <summary>
  /// Publish a message type to a named exchange (or equivalent pub/sub destination).
  /// </summary>
  void PublishToExchange<T>(WolverineOptions opts, string exchangeName, string? brokerName);

  /// <summary>
  /// Publish a message type to a named queue (point-to-point).
  /// </summary>
  void PublishToQueue<T>(WolverineOptions opts, string queueName, string? brokerName);

  /// <summary>
  /// Validate transport-specific options. Returns validation error messages, or empty if valid.
  /// </summary>
  IEnumerable<string> Validate(MessagingOptions options, IConfiguration configuration);
}