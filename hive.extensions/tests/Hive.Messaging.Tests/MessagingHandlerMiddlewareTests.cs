using FluentAssertions;
using Hive.Messaging.Middleware;
using Hive.Messaging.Telemetry;
using Hive.Messaging.Tests.TestFixtures;
using CloudTek.Testing;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Xunit;

namespace Hive.Messaging.Tests;

[Collection("MessagingMetrics")]
public class MessageHandlerMiddlewareTests : IDisposable
{
  private readonly MetricCollector<double> _durationCollector;
  private readonly MetricCollector<long> _countCollector;
  private readonly MetricCollector<long> _errorCollector;

  public MessageHandlerMiddlewareTests()
  {
    _durationCollector = new MetricCollector<double>(MessagingMeter.HandlerDuration);
    _countCollector = new MetricCollector<long>(MessagingMeter.HandlerCount);
    _errorCollector = new MetricCollector<long>(MessagingMeter.HandlerErrors);
  }

  [Fact]
  [UnitTest]
  public void GivenEnvelope_WhenBeforeCalled_ThenNoExceptionIsThrown()
  {
    var context = StubMessageContext.WithEnvelope("TestMessage", "queue://orders");

    var act = () => MessageHandlerMiddleware.Before(context);

    act.Should().NotThrow();
  }

  [Fact]
  [UnitTest]
  public void GivenSuccessfulHandling_WhenFinallyCalled_ThenHandlerCountIsIncremented()
  {
    var context = StubMessageContext.WithEnvelope("TestMessage", "queue://orders");

    var telemetry = MessageHandlerMiddleware.Before(context);
    MessageHandlerMiddleware.Finally(telemetry, exception: null);

    _countCollector.GetMeasurementSnapshot().Should().ContainSingle()
      .Which.Value.Should().Be(1);
  }

  [Fact]
  [UnitTest]
  public void GivenSuccessfulHandling_WhenFinallyCalled_ThenDurationIsRecorded()
  {
    var context = StubMessageContext.WithEnvelope("TestMessage", "queue://orders");

    var telemetry = MessageHandlerMiddleware.Before(context);
    Thread.Sleep(10);
    MessageHandlerMiddleware.Finally(telemetry, exception: null);

    _durationCollector.GetMeasurementSnapshot().Should().ContainSingle()
      .Which.Value.Should().BeGreaterThan(0);
  }

  [Fact]
  [UnitTest]
  public void GivenFailedHandling_WhenFinallyCalledWithException_ThenHandlerErrorsIsIncremented()
  {
    var context = StubMessageContext.WithEnvelope("TestMessage", "queue://orders");
    var exception = new InvalidOperationException("test error");

    var telemetry = MessageHandlerMiddleware.Before(context);
    MessageHandlerMiddleware.Finally(telemetry, exception);

    var snapshot = _errorCollector.GetMeasurementSnapshot();
    snapshot.Should().ContainSingle();
    snapshot[0].Value.Should().Be(1);
    snapshot[0].Tags["error.type"].Should().Be("InvalidOperationException");
  }

  [Fact]
  [UnitTest]
  public void GivenFailedHandling_WhenFinallyCalledWithException_ThenHandlerCountIsNotIncremented()
  {
    var context = StubMessageContext.WithEnvelope("TestMessage", "queue://orders");

    var telemetry = MessageHandlerMiddleware.Before(context);
    MessageHandlerMiddleware.Finally(telemetry, new InvalidOperationException("fail"));

    _countCollector.GetMeasurementSnapshot().Should().BeEmpty();
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
    _durationCollector.Dispose();
    _countCollector.Dispose();
    _errorCollector.Dispose();
  }
}