using Hive.Messaging.Transport;
using Wolverine;

namespace Hive.Messaging.Sending;

/// <summary>
/// Fluent builder for configuring message publish/send destinations.
/// </summary>
public sealed class MessageSenderBuilder
{
  private readonly WolverineOptions _options;
  private readonly IMessagingTransportProvider? _transportProvider;
  internal List<Action<WolverineOptions>> Registrations { get; } = [];

  internal MessageSenderBuilder(WolverineOptions options, IMessagingTransportProvider? transportProvider)
  {
    _options = options;
    _transportProvider = transportProvider;
  }

  /// <summary>
  /// Begins configuration of a publish route for the specified message type.
  /// </summary>
  /// <typeparam name="T">The message type to publish.</typeparam>
  /// <returns>A <see cref="PublishExpression{T}"/> for further configuration.</returns>
  public PublishExpression<T> Publish<T>()
  {
    return new PublishExpression<T>(this);
  }

  internal void Apply()
  {
    foreach (var registration in Registrations)
    {
      registration(_options);
    }
  }

  /// <summary>
  /// Fluent expression for configuring a publish route for a specific message type.
  /// </summary>
  /// <typeparam name="T">The message type being published.</typeparam>
  public sealed class PublishExpression<T>
  {
    private readonly MessageSenderBuilder _builder;
    private string? _brokerName;

    internal PublishExpression(MessageSenderBuilder builder)
    {
      _builder = builder;
    }

    /// <summary>
    /// Specifies the named broker to publish to.
    /// </summary>
    /// <param name="brokerName">The broker name from configuration.</param>
    public PublishExpression<T> OnBroker(string brokerName)
    {
      _brokerName = brokerName;
      return this;
    }

    /// <summary>
    /// Routes the message type to the specified exchange.
    /// </summary>
    /// <param name="exchangeName">The exchange name to publish to.</param>
    public MessageSenderBuilder ToExchange(string exchangeName)
    {
      var broker = _brokerName;
      var provider = _builder._transportProvider
        ?? throw new InvalidOperationException(
          "No transport provider configured. " +
          "Call UseRabbitMq() or another transport method, or configure transport via IConfiguration.");

      _builder.Registrations.Add(opts =>
        provider.PublishToExchange<T>(opts, exchangeName, broker));

      return _builder;
    }

    /// <summary>
    /// Routes the message type to the specified queue.
    /// </summary>
    /// <param name="queueName">The queue name to publish to.</param>
    public MessageSenderBuilder ToQueue(string queueName)
    {
      var broker = _brokerName;
      var provider = _builder._transportProvider
        ?? throw new InvalidOperationException(
          "No transport provider configured. " +
          "Call UseRabbitMq() or another transport method, or configure transport via IConfiguration.");

      _builder.Registrations.Add(opts =>
        provider.PublishToQueue<T>(opts, queueName, broker));

      return _builder;
    }
  }
}
