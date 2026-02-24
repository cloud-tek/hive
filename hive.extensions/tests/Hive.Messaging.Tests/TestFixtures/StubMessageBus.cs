using Wolverine;

namespace Hive.Messaging.Tests.TestFixtures;

internal sealed class StubMessageBus : IMessageBus
{
  public bool ThrowOnSend { get; set; }
  public bool ThrowOnPublish { get; set; }
  public bool ThrowOnBroadcast { get; set; }
  public int SendCount { get; private set; }
  public int PublishCount { get; private set; }
  public int BroadcastCount { get; private set; }

  public string? TenantId { get; set; }

  public ValueTask SendAsync<T>(T message, DeliveryOptions? options = null)
  {
    if (ThrowOnSend)
      throw new InvalidOperationException("Send failed");
    SendCount++;
    return ValueTask.CompletedTask;
  }

  public ValueTask PublishAsync<T>(T message, DeliveryOptions? options = null)
  {
    if (ThrowOnPublish)
      throw new InvalidOperationException("Publish failed");
    PublishCount++;
    return ValueTask.CompletedTask;
  }

  public ValueTask BroadcastToTopicAsync(string topicName, object message, DeliveryOptions? options = null)
  {
    if (ThrowOnBroadcast)
      throw new InvalidOperationException("Broadcast failed");
    BroadcastCount++;
    return ValueTask.CompletedTask;
  }

  public Task InvokeAsync(object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => Task.CompletedTask;

  public Task<T> InvokeAsync<T>(object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => throw new NotImplementedException();

  public Task InvokeAsync(object message, DeliveryOptions options, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => Task.CompletedTask;

  public Task<T> InvokeAsync<T>(object message, DeliveryOptions options, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => throw new NotImplementedException();

  public Task InvokeForTenantAsync(string tenantId, object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => Task.CompletedTask;

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
