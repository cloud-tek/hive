using Hive.Messaging.Configuration;
using Hive.Messaging.RabbitMq.Configuration;
using Hive.Messaging.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Wolverine;
using Wolverine.RabbitMQ;

namespace Hive.Messaging.RabbitMq;

/// <summary>
/// RabbitMQ transport provider for Hive.Messaging.
/// Binds RabbitMQ configuration from IConfiguration and configures Wolverine's RabbitMQ transport.
/// </summary>
public sealed class RabbitMqTransportProvider : IMessagingTransportProvider
{
  private readonly RabbitMqOptions? _builderOptions;

  internal RabbitMqTransportProvider() { }

  internal RabbitMqTransportProvider(RabbitMqOptions builderOptions)
  {
    _builderOptions = builderOptions;
  }

  /// <inheritdoc />
  public void ConfigureTransport(WolverineOptions opts, MessagingOptions options, IConfiguration configuration)
  {
    var rmqOptions = ResolveOptions(configuration);

    var rabbit = opts.UseRabbitMq(new Uri(rmqOptions.ConnectionUri!));
    if (rmqOptions.AutoProvision)
      rabbit.AutoProvision();

    rabbit.ConfigureListeners(listener =>
    {
      if (options.Handling.PrefetchCount.HasValue)
        listener.PreFetchCount((ushort)options.Handling.PrefetchCount.Value);
      if (options.Handling.ListenerCount.HasValue)
        listener.ListenerCount(options.Handling.ListenerCount.Value);
    });

    // Named brokers
    foreach (var (name, _) in options.NamedBrokers)
    {
      var brokerSection = configuration.GetSection($"{MessagingOptions.SectionKey}:NamedBrokers:{name}:RabbitMq");
      var brokerRmq = new RabbitMqOptions();
      brokerSection.Bind(brokerRmq);

      var broker = new BrokerName(name);
      opts.AddNamedRabbitMqBroker(broker, f => f.Uri = new Uri(brokerRmq.ConnectionUri!));
    }
  }

  /// <inheritdoc />
  public void ListenToQueue(WolverineOptions opts, string queueName, string? brokerName,
    int? prefetchCount, int? listenerCount)
  {
    if (brokerName != null)
    {
      var listener = opts.ListenToRabbitQueueOnNamedBroker(
        new BrokerName(brokerName), queueName);

      if (prefetchCount.HasValue)
        listener.PreFetchCount((ushort)prefetchCount.Value);
      if (listenerCount.HasValue)
        listener.ListenerCount(listenerCount.Value);
    }
    else
    {
      var listener = opts.ListenToRabbitQueue(queueName);

      if (prefetchCount.HasValue)
        listener.PreFetchCount((ushort)prefetchCount.Value);
      if (listenerCount.HasValue)
        listener.ListenerCount(listenerCount.Value);
    }
  }

  /// <inheritdoc />
  public void PublishToExchange<T>(WolverineOptions opts, string exchangeName, string? brokerName)
  {
    if (brokerName != null)
    {
      opts.PublishMessage<T>()
        .ToRabbitExchangeOnNamedBroker(new BrokerName(brokerName), exchangeName);
    }
    else
    {
      opts.PublishMessage<T>()
        .ToRabbitExchange(exchangeName);
    }
  }

  /// <inheritdoc />
  public void PublishToQueue<T>(WolverineOptions opts, string queueName, string? brokerName)
  {
    if (brokerName != null)
    {
      opts.PublishMessage<T>()
        .ToRabbitQueueOnNamedBroker(new BrokerName(brokerName), queueName);
    }
    else
    {
      opts.PublishMessage<T>()
        .ToRabbitQueue(queueName);
    }
  }

  /// <inheritdoc />
  public IHealthChecksBuilder ConfigureHealthChecks(
    IHealthChecksBuilder builder, MessagingOptions options, IConfiguration configuration)
  {
    var rmqOptions = ResolveOptions(configuration);

    if (!string.IsNullOrEmpty(rmqOptions.ConnectionUri))
    {
      var uri = rmqOptions.ConnectionUri;
      IConnection? connection = null;
      builder.AddRabbitMQ(
        factory: async _ =>
        {
          if (connection is not { IsOpen: true })
          {
            var factory = new ConnectionFactory { Uri = new Uri(uri) };
            connection = await factory.CreateConnectionAsync();
          }
          return connection;
        },
        name: "rabbitmq");
    }

    foreach (var (name, _) in options.NamedBrokers)
    {
      var brokerSection = configuration.GetSection($"{MessagingOptions.SectionKey}:NamedBrokers:{name}:RabbitMq");
      var brokerRmq = new RabbitMqOptions();
      brokerSection.Bind(brokerRmq);

      if (!string.IsNullOrEmpty(brokerRmq.ConnectionUri))
      {
        var brokerUri = brokerRmq.ConnectionUri;
        IConnection? brokerConnection = null;
        builder.AddRabbitMQ(
          factory: async _ =>
          {
            if (brokerConnection is not { IsOpen: true })
            {
              var factory = new ConnectionFactory { Uri = new Uri(brokerUri) };
              brokerConnection = await factory.CreateConnectionAsync();
            }
            return brokerConnection;
          },
          name: $"rabbitmq:{name}");
      }
    }

    return builder;
  }

  /// <inheritdoc />
  public IEnumerable<string> Validate(MessagingOptions options, IConfiguration configuration)
  {
    var errors = new List<string>();
    var rmqOptions = ResolveOptions(configuration);

    if (options.Transport == MessagingTransport.RabbitMQ)
    {
      if (string.IsNullOrEmpty(rmqOptions.ConnectionUri))
        errors.Add("ConnectionUri is required when Transport is RabbitMQ");
      else if (!Uri.TryCreate(rmqOptions.ConnectionUri, UriKind.Absolute, out _))
        errors.Add("ConnectionUri must be a valid absolute URI");
    }

    foreach (var (name, _) in options.NamedBrokers)
    {
      var brokerSection = configuration.GetSection($"{MessagingOptions.SectionKey}:NamedBrokers:{name}:RabbitMq");
      var brokerRmq = new RabbitMqOptions();
      brokerSection.Bind(brokerRmq);

      if (string.IsNullOrEmpty(brokerRmq.ConnectionUri))
        errors.Add($"ConnectionUri is required for named broker '{name}'");
      else if (!Uri.TryCreate(brokerRmq.ConnectionUri, UriKind.Absolute, out _))
        errors.Add($"ConnectionUri must be a valid absolute URI for named broker '{name}'");
    }

    return errors;
  }

  private RabbitMqOptions ResolveOptions(IConfiguration configuration)
  {
    if (_builderOptions != null)
      return _builderOptions;

    var section = configuration.GetSection($"{MessagingOptions.SectionKey}:RabbitMq");
    var options = new RabbitMqOptions();
    section.Bind(options);
    return options;
  }
}