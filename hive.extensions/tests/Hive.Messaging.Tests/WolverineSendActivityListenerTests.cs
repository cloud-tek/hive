using System.Diagnostics;
using FluentAssertions;
using Hive.Messaging.Telemetry;
using CloudTek.Testing;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Xunit;

namespace Hive.Messaging.Tests;

[Collection("MessagingMetrics")]
public class WolverineSendActivityListenerTests : IAsyncLifetime, IDisposable
{
  private readonly WolverineSendActivityListener _listener;
  private readonly ActivitySource _wolverineSource;
  private readonly MetricCollector<long> _sentCollector;

  public WolverineSendActivityListenerTests()
  {
    _listener = new WolverineSendActivityListener();
    _wolverineSource = new ActivitySource("Wolverine");
    _sentCollector = new MetricCollector<long>(MessagingMeter.MessagesSent);
  }

  public async Task InitializeAsync()
  {
    await _listener.StartAsync(CancellationToken.None);
  }

  public async Task DisposeAsync()
  {
    await _listener.StopAsync(CancellationToken.None);
  }

  [Fact]
  [UnitTest]
  public void GivenSendActivity_WhenNotTracked_ThenMessagesSentIncremented()
  {
    var activity = _wolverineSource.StartActivity("send OrderCreated");
    activity!.SetTag("messaging.message.type", "OrderCreated");
    activity.Stop();

    _sentCollector.GetMeasurementSnapshot().Should().ContainSingle()
      .Which.Value.Should().Be(1);
  }

  [Fact]
  [UnitTest]
  public void GivenSendActivity_WhenAlreadyTracked_ThenMessagesSentNotIncremented()
  {
    var activity = _wolverineSource.StartActivity("send OrderCreated");
    activity!.SetTag("messaging.message.type", "OrderCreated");
    activity.SetTag("hive.messaging.tracked", true);
    activity.Stop();

    _sentCollector.GetMeasurementSnapshot().Should().BeEmpty();
  }

  [Fact]
  [UnitTest]
  public void GivenPublishActivity_WhenNotTracked_ThenMessagesSentIncremented()
  {
    var activity = _wolverineSource.StartActivity("publish OrderCreated");
    activity!.SetTag("messaging.message.type", "OrderCreated");
    activity.Stop();

    _sentCollector.GetMeasurementSnapshot().Should().ContainSingle()
      .Which.Value.Should().Be(1);
  }

  [Fact]
  [UnitTest]
  public void GivenNonSendActivity_WhenStopped_ThenMessagesSentNotIncremented()
  {
    var activity = _wolverineSource.StartActivity("handle OrderCreated");
    activity!.SetTag("messaging.message.type", "OrderCreated");
    activity.Stop();

    _sentCollector.GetMeasurementSnapshot().Should().BeEmpty();
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
    _wolverineSource.Dispose();
    _sentCollector.Dispose();
    _listener.Dispose();
  }
}