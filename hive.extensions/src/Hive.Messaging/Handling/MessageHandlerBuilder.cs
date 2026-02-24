using Hive.Messaging.Transport;
using Wolverine;

namespace Hive.Messaging.Handling;

public sealed class MessageHandlerBuilder
{
  private readonly WolverineOptions _options;
  private readonly IMessagingTransportProvider? _transportProvider;
  internal List<Action<WolverineOptions>> Registrations { get; } = [];

  internal MessageHandlerBuilder(WolverineOptions options, IMessagingTransportProvider? transportProvider)
  {
    _options = options;
    _transportProvider = transportProvider;
  }

  public ListenerExpression ListenToQueue(string queueName)
  {
    return new ListenerExpression(this, queueName);
  }

  internal void Apply()
  {
    foreach (var registration in Registrations)
    {
      registration(_options);
    }
  }

  public sealed class ListenerExpression
  {
    private readonly MessageHandlerBuilder _builder;
    private readonly string _queueName;
    private string? _brokerName;
    private int? _prefetchCount;
    private int? _listenerCount;

    internal ListenerExpression(MessageHandlerBuilder builder, string queueName)
    {
      _builder = builder;
      _queueName = queueName;

      builder.Registrations.Add(ApplyToOptions);
    }

    public ListenerExpression OnBroker(string brokerName)
    {
      _brokerName = brokerName;
      return this;
    }

    public ListenerExpression Prefetch(int count)
    {
      _prefetchCount = count;
      return this;
    }

    public ListenerExpression ListenerCount(int count)
    {
      _listenerCount = count;
      return this;
    }

    public ListenerExpression Sequential()
    {
      _prefetchCount = 1;
      _listenerCount = 1;
      return this;
    }

    private void ApplyToOptions(WolverineOptions opts)
    {
      var provider = _builder._transportProvider
        ?? throw new InvalidOperationException(
          "No transport provider configured. " +
          "Call UseRabbitMq() or another transport method, or configure transport via IConfiguration.");

      provider.ListenToQueue(opts, _queueName, _brokerName, _prefetchCount, _listenerCount);
    }
  }
}
