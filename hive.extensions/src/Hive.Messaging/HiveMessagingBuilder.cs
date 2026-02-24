using Hive.Messaging.Configuration;
using Hive.Messaging.Handling;
using Wolverine;

namespace Hive.Messaging;

public sealed class HiveMessagingBuilder : HiveMessagingSendBuilder
{
  internal HiveMessagingBuilder(WolverineOptions wolverineOptions, MessagingOptions messagingOptions)
    : base(wolverineOptions, messagingOptions)
  {
  }

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

  public new HiveMessagingBuilder UseInMemoryTransport()
  {
    base.UseInMemoryTransport();
    return this;
  }

  public new HiveMessagingBuilder WithSerialization(Action<Serialization.SerializationBuilder> configure)
  {
    base.WithSerialization(configure);
    return this;
  }

  public new HiveMessagingBuilder WithSending(Action<Sending.MessageSenderBuilder> configure)
  {
    base.WithSending(configure);
    return this;
  }

  public new HiveMessagingBuilder ConfigureWolverine(Action<WolverineOptions> configure)
  {
    base.ConfigureWolverine(configure);
    return this;
  }
}
