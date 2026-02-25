using System.Diagnostics;
using Wolverine;

namespace Hive.Messaging.Telemetry;

internal sealed class TelemetryMessageBus : IMessageBus
{
  private const string TrackedTag = "hive.messaging.tracked";
  private readonly IMessageBus _inner;

  public TelemetryMessageBus(IMessageBus inner)
  {
    _inner = inner;
  }

  public string? TenantId
  {
    get => _inner.TenantId;
    set => _inner.TenantId = value;
  }

  public async ValueTask SendAsync<T>(T message, DeliveryOptions? options = null)
  {
    TagCurrentActivity();
    var tags = CreateSendTags(message);
    try
    {
      await _inner.SendAsync(message, options);
      MessagingMeter.MessagesSent.Add(1, tags);
    }
    catch (Exception ex)
    {
      var errorTags = tags;
      errorTags.Add("error.type", ex.GetType().Name);
      MessagingMeter.SendErrors.Add(1, errorTags);
      throw;
    }
  }

  public async ValueTask PublishAsync<T>(T message, DeliveryOptions? options = null)
  {
    TagCurrentActivity();
    var tags = CreateSendTags(message);
    try
    {
      await _inner.PublishAsync(message, options);
      MessagingMeter.MessagesSent.Add(1, tags);
    }
    catch (Exception ex)
    {
      var errorTags = tags;
      errorTags.Add("error.type", ex.GetType().Name);
      MessagingMeter.SendErrors.Add(1, errorTags);
      throw;
    }
  }

  public async ValueTask BroadcastToTopicAsync(string topicName, object message, DeliveryOptions? options = null)
  {
    TagCurrentActivity();
    var tags = new TagList
    {
      { "messaging.message.type", message.GetType().Name },
      { "messaging.destination", topicName }
    };
    try
    {
      await _inner.BroadcastToTopicAsync(topicName, message, options);
      MessagingMeter.MessagesSent.Add(1, tags);
    }
    catch (Exception ex)
    {
      var errorTags = tags;
      errorTags.Add("error.type", ex.GetType().Name);
      MessagingMeter.SendErrors.Add(1, errorTags);
      throw;
    }
  }

  public Task InvokeAsync(object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => _inner.InvokeAsync(message, cancellation, timeout);

  public Task<T> InvokeAsync<T>(object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => _inner.InvokeAsync<T>(message, cancellation, timeout);

  public Task InvokeAsync(object message, DeliveryOptions options, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => _inner.InvokeAsync(message, options, cancellation, timeout);

  public Task<T> InvokeAsync<T>(object message, DeliveryOptions options, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => _inner.InvokeAsync<T>(message, options, cancellation, timeout);

  public Task InvokeForTenantAsync(string tenantId, object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => _inner.InvokeForTenantAsync(tenantId, message, cancellation, timeout);

  public Task<T> InvokeForTenantAsync<T>(string tenantId, object message, CancellationToken cancellation = default, TimeSpan? timeout = null)
    => _inner.InvokeForTenantAsync<T>(tenantId, message, cancellation, timeout);

  public IDestinationEndpoint EndpointFor(string endpointName)
    => _inner.EndpointFor(endpointName);

  public IDestinationEndpoint EndpointFor(Uri uri)
    => _inner.EndpointFor(uri);

  public IReadOnlyList<Envelope> PreviewSubscriptions(object message)
    => _inner.PreviewSubscriptions(message);

  public IReadOnlyList<Envelope> PreviewSubscriptions(object message, DeliveryOptions options)
    => _inner.PreviewSubscriptions(message, options);

  private static void TagCurrentActivity()
  {
    Activity.Current?.SetTag(TrackedTag, true);
  }

  private static TagList CreateSendTags<T>(T message)
  {
    return new TagList
    {
      { "messaging.message.type", typeof(T).Name }
    };
  }
}