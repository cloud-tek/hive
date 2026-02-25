using Hive.Messaging.Configuration;
using Hive.Messaging.Handling;
using Wolverine;

namespace Hive.Messaging;

/// <summary>
/// Fluent builder for configuring Hive messaging with both sending and handling capabilities.
/// </summary>
public sealed class HiveMessagingBuilder : HiveMessagingSendBuilder
{
  internal HiveMessagingBuilder(WolverineOptions wolverineOptions, MessagingOptions messagingOptions)
    : base(wolverineOptions, messagingOptions)
  {
  }

  /// <summary>
  /// Configures message handling (queue listeners) for this service.
  /// </summary>
  /// <param name="configure">Action to configure message handler listeners.</param>
  public HiveMessagingBuilder WithHandling(Action<MessageHandlerBuilder> configure)
  {
    AddDeferredRegistration((opts, provider) =>
    {
      var builder = new MessageHandlerBuilder(opts, provider);
      configure(builder);
      builder.Apply();
    });
    return this;
  }

  /// <summary>
  /// Configures the service to use the in-memory transport (Wolverine built-in).
  /// </summary>
  public new HiveMessagingBuilder UseInMemoryTransport()
  {
    base.UseInMemoryTransport();
    return this;
  }

  /// <summary>
  /// Configures message serialization format.
  /// </summary>
  /// <param name="configure">Action to configure serialization.</param>
  public new HiveMessagingBuilder WithSerialization(Action<Serialization.SerializationBuilder> configure)
  {
    base.WithSerialization(configure);
    return this;
  }

  /// <summary>
  /// Configures message sending (publish/send destinations).
  /// </summary>
  /// <param name="configure">Action to configure message senders.</param>
  public new HiveMessagingBuilder WithSending(Action<Sending.MessageSenderBuilder> configure)
  {
    base.WithSending(configure);
    return this;
  }

  /// <summary>
  /// Provides direct access to Wolverine options for advanced configuration.
  /// </summary>
  /// <param name="configure">Action to configure Wolverine options directly.</param>
  public new HiveMessagingBuilder ConfigureWolverine(Action<WolverineOptions> configure)
  {
    base.ConfigureWolverine(configure);
    return this;
  }
}