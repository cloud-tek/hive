using System.Diagnostics;
using FluentAssertions;
using Hive.Messaging.Telemetry;
using Hive.Messaging.Tests.TestFixtures;
using Hive.Testing;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Xunit;

namespace Hive.Messaging.Tests;

[Collection("MessagingMetrics")]
public class TelemetryMessageBusTests : IDisposable
{
  private readonly MetricCollector<long> _sentCollector;
  private readonly MetricCollector<long> _errorCollector;

  public TelemetryMessageBusTests()
  {
    _sentCollector = new MetricCollector<long>(MessagingMeter.MessagesSent);
    _errorCollector = new MetricCollector<long>(MessagingMeter.SendErrors);
  }

  [Fact]
  [UnitTest]
  public async Task GivenSendAsync_WhenSucceeds_ThenMessagesSentIncremented()
  {
    var inner = new StubMessageBus();
    var bus = new TelemetryMessageBus(inner);

    await bus.SendAsync(new TestMessage("hello"));

    _sentCollector.GetMeasurementSnapshot().Should().ContainSingle()
      .Which.Value.Should().Be(1);
  }

  [Fact]
  [UnitTest]
  public async Task GivenSendAsync_WhenThrows_ThenSendErrorsIncrementedAndRethrown()
  {
    var inner = new StubMessageBus { ThrowOnSend = true };
    var bus = new TelemetryMessageBus(inner);

    var act = async () => await bus.SendAsync(new TestMessage("fail"));

    await act.Should().ThrowAsync<InvalidOperationException>();
    _errorCollector.GetMeasurementSnapshot().Should().ContainSingle();
    _sentCollector.GetMeasurementSnapshot().Should().BeEmpty();
  }

  [Fact]
  [UnitTest]
  public async Task GivenPublishAsync_WhenSucceeds_ThenMessagesSentIncremented()
  {
    var inner = new StubMessageBus();
    var bus = new TelemetryMessageBus(inner);

    await bus.PublishAsync(new TestMessage("hello"));

    _sentCollector.GetMeasurementSnapshot().Should().ContainSingle()
      .Which.Value.Should().Be(1);
  }

  [Fact]
  [UnitTest]
  public async Task GivenPublishAsync_WhenThrows_ThenSendErrorsIncrementedAndRethrown()
  {
    var inner = new StubMessageBus { ThrowOnPublish = true };
    var bus = new TelemetryMessageBus(inner);

    var act = async () => await bus.PublishAsync(new TestMessage("fail"));

    await act.Should().ThrowAsync<InvalidOperationException>();
    _errorCollector.GetMeasurementSnapshot().Should().ContainSingle();
  }

  [Fact]
  [UnitTest]
  public async Task GivenBroadcastToTopicAsync_WhenSucceeds_ThenMessagesSentIncremented()
  {
    var inner = new StubMessageBus();
    var bus = new TelemetryMessageBus(inner);

    await bus.BroadcastToTopicAsync("orders", new TestMessage("hello"));

    _sentCollector.GetMeasurementSnapshot().Should().ContainSingle()
      .Which.Value.Should().Be(1);
  }

  [Fact]
  [UnitTest]
  public async Task GivenBroadcastToTopicAsync_WhenThrows_ThenSendErrorsWithErrorType()
  {
    var inner = new StubMessageBus { ThrowOnBroadcast = true };
    var bus = new TelemetryMessageBus(inner);

    var act = async () => await bus.BroadcastToTopicAsync("orders", new TestMessage("fail"));

    await act.Should().ThrowAsync<InvalidOperationException>();
    var snapshot = _errorCollector.GetMeasurementSnapshot();
    snapshot.Should().ContainSingle();
    snapshot[0].Tags["error.type"].Should().Be("InvalidOperationException");
  }

  [Fact]
  [UnitTest]
  public async Task GivenSendAsync_WhenCalled_ThenCurrentActivityTaggedWithTracked()
  {
    using var source = new ActivitySource("test-telemetry-bus");
    using var listener = new ActivityListener
    {
      ShouldListenTo = s => s.Name == "test-telemetry-bus",
      Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
    };
    ActivitySource.AddActivityListener(listener);

    using var activity = source.StartActivity("test-send");
    var inner = new StubMessageBus();
    var bus = new TelemetryMessageBus(inner);

    await bus.SendAsync(new TestMessage("hello"));

    activity!.GetTagItem("hive.messaging.tracked").Should().Be(true);
  }

  [Fact]
  [UnitTest]
  public async Task GivenSendAsync_WhenSucceeds_ThenDelegatesCallToInner()
  {
    var inner = new StubMessageBus();
    var bus = new TelemetryMessageBus(inner);

    await bus.SendAsync(new TestMessage("hello"));

    inner.SendCount.Should().Be(1);
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
    _sentCollector.Dispose();
    _errorCollector.Dispose();
  }
}