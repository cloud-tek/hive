using Hive.Messaging.Configuration;
using Hive.Messaging.Sending;
using Hive.Messaging.Serialization;
using Hive.Messaging.Transport;
using Wolverine;

namespace Hive.Messaging;

public class HiveMessagingSendBuilder
{
  internal WolverineOptions WolverineOptions { get; }
  internal MessagingOptions MessagingOptions { get; }
  internal IMessagingTransportProvider? TransportProvider { get; set; }
  internal List<Action<WolverineOptions>> EscapeHatchActions { get; } = [];
  private readonly List<Action<WolverineOptions, IMessagingTransportProvider?>> _deferredRegistrations = [];

  internal void AddDeferredRegistration(Action<WolverineOptions, IMessagingTransportProvider?> registration)
    => _deferredRegistrations.Add(registration);

  internal HiveMessagingSendBuilder(WolverineOptions wolverineOptions, MessagingOptions messagingOptions)
  {
    WolverineOptions = wolverineOptions;
    MessagingOptions = messagingOptions;
  }

  public HiveMessagingSendBuilder UseInMemoryTransport()
  {
    MessagingOptions.Transport = MessagingTransport.InMemory;
    TransportProvider = null;
    return this;
  }

  public HiveMessagingSendBuilder WithSerialization(Action<SerializationBuilder> configure)
  {
    var builder = new SerializationBuilder(WolverineOptions);
    configure(builder);
    MessagingOptions.Serialization = builder.Serialization;
    return this;
  }

  public HiveMessagingSendBuilder WithSending(Action<MessageSenderBuilder> configure)
  {
    _deferredRegistrations.Add((opts, provider) =>
    {
      var builder = new MessageSenderBuilder(opts, provider);
      configure(builder);
      builder.Apply();
    });
    return this;
  }

  public HiveMessagingSendBuilder ConfigureWolverine(Action<WolverineOptions> configure)
  {
    EscapeHatchActions.Add(configure);
    return this;
  }

  internal void ApplyEscapeHatch()
  {
    foreach (var action in EscapeHatchActions)
    {
      action(WolverineOptions);
    }
  }

  internal void ApplyDeferredRegistrations(IMessagingTransportProvider? provider)
  {
    foreach (var registration in _deferredRegistrations)
    {
      registration(WolverineOptions, provider);
    }
  }
}
