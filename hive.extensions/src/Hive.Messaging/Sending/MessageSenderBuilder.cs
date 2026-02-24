using Hive.Messaging.Transport;
using Wolverine;

namespace Hive.Messaging.Sending;

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

  public sealed class PublishExpression<T>
  {
    private readonly MessageSenderBuilder _builder;
    private string? _brokerName;

    internal PublishExpression(MessageSenderBuilder builder)
    {
      _builder = builder;
    }

    public PublishExpression<T> OnBroker(string brokerName)
    {
      _brokerName = brokerName;
      return this;
    }

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
