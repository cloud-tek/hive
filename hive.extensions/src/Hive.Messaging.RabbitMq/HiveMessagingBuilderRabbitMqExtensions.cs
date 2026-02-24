using Hive.Messaging.Configuration;
using Hive.Messaging.RabbitMq.Configuration;
using Hive.Messaging.RabbitMq.Transport;

namespace Hive.Messaging.RabbitMq;

/// <summary>
/// Extension methods for configuring RabbitMQ transport on Hive messaging builders.
/// </summary>
public static class HiveMessagingBuilderRabbitMqExtensions
{
  /// <summary>
  /// Configure the RabbitMQ transport. Connection details are read from IConfiguration
  /// at the "Hive:Messaging:RabbitMq" section.
  /// </summary>
  public static TBuilder UseRabbitMq<TBuilder>(this TBuilder builder)
    where TBuilder : HiveMessagingSendBuilder
  {
    builder.MessagingOptions.Transport = MessagingTransport.RabbitMQ;
    builder.TransportProvider = new RabbitMqTransportProvider();
    return builder;
  }

  /// <summary>
  /// Configure the RabbitMQ transport with a connection URI.
  /// </summary>
  public static TBuilder UseRabbitMq<TBuilder>(this TBuilder builder, string connectionUri)
    where TBuilder : HiveMessagingSendBuilder
  {
    builder.MessagingOptions.Transport = MessagingTransport.RabbitMQ;
    builder.TransportProvider = new RabbitMqTransportProvider(
      new RabbitMqOptions { ConnectionUri = connectionUri });
    return builder;
  }

  /// <summary>
  /// Configure the RabbitMQ transport with a builder callback.
  /// </summary>
  public static TBuilder UseRabbitMq<TBuilder>(
    this TBuilder builder, Action<RabbitMqTransportBuilder> configure)
    where TBuilder : HiveMessagingSendBuilder
  {
    builder.MessagingOptions.Transport = MessagingTransport.RabbitMQ;
    var transportBuilder = new RabbitMqTransportBuilder();
    configure(transportBuilder);
    builder.TransportProvider = new RabbitMqTransportProvider(transportBuilder.Options);
    return builder;
  }

  /// <summary>
  /// Configure a named RabbitMQ broker with a builder callback.
  /// </summary>
  public static TBuilder UseRabbitMq<TBuilder>(
    this TBuilder builder, string brokerName, Action<RabbitMqTransportBuilder> configure)
    where TBuilder : HiveMessagingSendBuilder
  {
    var transportBuilder = new RabbitMqTransportBuilder();
    configure(transportBuilder);
    builder.MessagingOptions.NamedBrokers[brokerName] = new NamedBrokerOptions();
    builder.TransportProvider ??= new RabbitMqTransportProvider();
    return builder;
  }
}
