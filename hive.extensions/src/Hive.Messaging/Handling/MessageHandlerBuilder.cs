using Hive.Messaging.Transport;
using Wolverine;

namespace Hive.Messaging.Handling;

/// <summary>
/// Fluent builder for configuring message handler queue listeners.
/// </summary>
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

  /// <summary>
  /// Configures the handler to listen to the specified queue.
  /// </summary>
  /// <param name="queueName">The name of the queue to listen on.</param>
  /// <returns>A <see cref="ListenerExpression"/> for further configuration.</returns>
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

  /// <summary>
  /// Fluent expression for configuring a queue listener.
  /// </summary>
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

    /// <summary>
    /// Specifies the named broker to use for this listener.
    /// </summary>
    /// <param name="brokerName">The broker name from configuration.</param>
    public ListenerExpression OnBroker(string brokerName)
    {
      _brokerName = brokerName;
      return this;
    }

    /// <summary>
    /// Sets the prefetch count for the listener.
    /// </summary>
    /// <param name="count">The number of messages to prefetch.</param>
    public ListenerExpression Prefetch(int count)
    {
      _prefetchCount = count;
      return this;
    }

    /// <summary>
    /// Sets the number of concurrent listeners for the queue.
    /// </summary>
    /// <param name="count">The number of listeners.</param>
    public ListenerExpression ListenerCount(int count)
    {
      _listenerCount = count;
      return this;
    }

    /// <summary>
    /// Configures the listener for sequential (single-threaded) processing.
    /// </summary>
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