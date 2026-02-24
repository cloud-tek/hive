using Wolverine;

namespace Hive.Messaging.Tests.TestFixtures;

internal sealed class StubMessageContext : IMessageContext
{
  public Envelope? Envelope { get; init; }

  public string? CorrelationId { get; set; }

  public static StubMessageContext WithEnvelope(string messageType, string destination)
  {
    return new StubMessageContext
    {
      Envelope = new Envelope
      {
        MessageType = messageType,
        Destination = new Uri(destination)
      }
    };
  }

  // IMessageContext members
  public ValueTask RespondToSenderAsync(object response)
    => throw new NotImplementedException();

  public Task ReScheduleCurrentAsync(DateTimeOffset scheduledTime)
    => throw new NotImplementedException();

  // IMessageBus members
  public string? TenantId { get; set; }

  public ValueTask SendAsync<T>(T message, DeliveryOptions? options = null)
    => throw new NotImplementedException();

  public ValueTask PublishAsync<T>(T message, DeliveryOptions? options = null)
    => throw new NotImplementedException();

  public ValueTask BroadcastToTopicAsync(string topicName, object message, DeliveryOptions? options = null)
    => throw new NotImplementedException();

  public Task InvokeAsync(object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => throw new NotImplementedException();

  public Task<T> InvokeAsync<T>(object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => throw new NotImplementedException();

  public Task InvokeAsync(object message, DeliveryOptions options, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => throw new NotImplementedException();

  public Task<T> InvokeAsync<T>(object message, DeliveryOptions options, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => throw new NotImplementedException();

  public Task InvokeForTenantAsync(string tenantId, object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => throw new NotImplementedException();

  public Task<T> InvokeForTenantAsync<T>(string tenantId, object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => throw new NotImplementedException();

  public IDestinationEndpoint EndpointFor(string endpointName)
    => throw new NotImplementedException();

  public IDestinationEndpoint EndpointFor(Uri uri)
    => throw new NotImplementedException();

  public IReadOnlyList<Envelope> PreviewSubscriptions(object message)
    => throw new NotImplementedException();

  public IReadOnlyList<Envelope> PreviewSubscriptions(object message, DeliveryOptions options)
    => throw new NotImplementedException();
}
