using FluentAssertions;
using Hive.Messaging.Middleware;
using Hive.Messaging.Telemetry;
using Hive.Messaging.Tests.TestFixtures;
using Hive.Testing;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Xunit;

namespace Hive.Messaging.Tests;

[Collection("MessagingMetrics")]
public class MessagingHandlerMiddlewareTests : IDisposable
{
  private readonly MetricCollector<double> _durationCollector;
  private readonly MetricCollector<long> _countCollector;
  private readonly MetricCollector<long> _errorCollector;

  public MessagingHandlerMiddlewareTests()
  {
    _durationCollector = new MetricCollector<double>(MessagingMeter.HandlerDuration);
    _countCollector = new MetricCollector<long>(MessagingMeter.HandlerCount);
    _errorCollector = new MetricCollector<long>(MessagingMeter.HandlerErrors);
  }

  [Fact]
  [UnitTest]
  public void GivenEnvelope_WhenBeforeCalled_ThenNoExceptionIsThrown()
  {
    var middleware = new MessagingHandlerMiddleware();
    var context = StubMessageContext.WithEnvelope("TestMessage", "queue://orders");

    var act = () => middleware.Before(context);

    act.Should().NotThrow();
  }

  [Fact]
  [UnitTest]
  public void GivenSuccessfulHandling_WhenFinallyCalled_ThenHandlerCountIsIncremented()
  {
    var middleware = new MessagingHandlerMiddleware();
    var context = StubMessageContext.WithEnvelope("TestMessage", "queue://orders");

    middleware.Before(context);
    middleware.Finally(context, exception: null);

    _countCollector.GetMeasurementSnapshot().Should().ContainSingle()
      .Which.Value.Should().Be(1);
  }

  [Fact]
  [UnitTest]
  public void GivenSuccessfulHandling_WhenFinallyCalled_ThenDurationIsRecorded()
  {
    var middleware = new MessagingHandlerMiddleware();
    var context = StubMessageContext.WithEnvelope("TestMessage", "queue://orders");

    middleware.Before(context);
    Thread.Sleep(10);
    middleware.Finally(context, exception: null);

    _durationCollector.GetMeasurementSnapshot().Should().ContainSingle()
      .Which.Value.Should().BeGreaterThan(0);
  }

  [Fact]
  [UnitTest]
  public void GivenFailedHandling_WhenFinallyCalledWithException_ThenHandlerErrorsIsIncremented()
  {
    var middleware = new MessagingHandlerMiddleware();
    var context = StubMessageContext.WithEnvelope("TestMessage", "queue://orders");
    var exception = new InvalidOperationException("test error");

    middleware.Before(context);
    middleware.Finally(context, exception);

    var snapshot = _errorCollector.GetMeasurementSnapshot();
    snapshot.Should().ContainSingle();
    snapshot[0].Value.Should().Be(1);
    snapshot[0].Tags["error.type"].Should().Be("InvalidOperationException");
  }

  [Fact]
  [UnitTest]
  public void GivenFailedHandling_WhenFinallyCalledWithException_ThenHandlerCountIsNotIncremented()
  {
    var middleware = new MessagingHandlerMiddleware();
    var context = StubMessageContext.WithEnvelope("TestMessage", "queue://orders");

    middleware.Before(context);
    middleware.Finally(context, new InvalidOperationException("fail"));

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
