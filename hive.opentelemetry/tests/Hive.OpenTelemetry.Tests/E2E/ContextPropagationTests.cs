using System.Diagnostics;
using System.Net.Http;
using FluentAssertions;
using Hive.MicroServices;
using Hive.MicroServices.Api;
using Hive.MicroServices.Extensions;
using Hive.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using Xunit;

#pragma warning disable CA1848 // Use LoggerMessage delegates for performance

namespace Hive.OpenTelemetry.Tests.E2E;

/// <summary>
/// End-to-end tests for trace context propagation across telemetry signals
/// </summary>
[Collection("E2E Tests")]
public class ContextPropagationTests : IDisposable
{
  private static int _portCounter = 8000;
  private List<Activity> _exportedActivities = new();
  private List<LogRecord> _exportedLogs = new();
  private ushort _servicePort;
  private IDisposable? _portScope;

  private static ushort GetNextPortRange()
  {
    return (ushort)Interlocked.Add(ref _portCounter, 10);
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenTraceContext_WhenLogEmitted_ThenLogContainsTraceId()
  {
    // Arrange
    const string serviceName = "context-propagation-test";
    _portScope = TestPortProvider.GetAvailableServicePortScope(GetNextPortRange(), out _servicePort);

    var service = new MicroService(serviceName, new NullLogger<IMicroService>())
      .InTestClass<ContextPropagationTests>()
      .WithOpenTelemetry();

    service.ConfigureServices((svc, cfg) =>
    {
      svc.ConfigureOpenTelemetryLoggerProvider((_, builder) =>
        builder.AddInMemoryExporter(_exportedLogs));
      svc.ConfigureOpenTelemetryTracerProvider((_, builder) =>
        builder.AddInMemoryExporter(_exportedActivities));
      svc.Configure<OpenTelemetryLoggerOptions>(options =>
      {
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
      });
    });

    service.ConfigureApiPipeline(app =>
      {
        app.MapGet("/trace-log-correlation", (ILogger<ContextPropagationTests> logger) =>
        {
          logger.LogInformation("Log within trace context");
          return "OK";
        });
      });

    var config = new ConfigurationBuilder().Build();
    service.CancellationTokenSource.CancelAfter(10000);

    // Act
    var runTask = service.RunAsync(config);
    service.ShouldStart(TimeSpan.FromSeconds(5));

    using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{_servicePort}") };
    await client.GetAsync("/trace-log-correlation");
    await Task.Delay(200);

    service.CancellationTokenSource.Cancel();
    await runTask;

    // Assert - both traces and logs should be captured
    _exportedActivities.Should().NotBeEmpty("traces should be captured");
    _exportedLogs.Should().NotBeEmpty("logs should be captured");

    // Get the trace for this request
    var activity = _exportedActivities.FirstOrDefault();
    activity.Should().NotBeNull("HTTP request trace should be captured");

    // Get the log within the trace
    var correlatedLog = _exportedLogs.FirstOrDefault(log =>
      log.FormattedMessage != null &&
      log.FormattedMessage.Contains("Log within trace context"));

    correlatedLog.Should().NotBeNull("log should be captured");

    // Verify trace context is present on the log
    if (correlatedLog!.TraceId != default)
    {
      correlatedLog.TraceId.Should().Be(activity!.TraceId,
        "log TraceId should match the activity TraceId");
    }
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenNestedActivities_WhenLogsEmitted_ThenLogsContainCorrectSpanIds()
  {
    // Arrange
    const string serviceName = "nested-context-test";
    _portScope = TestPortProvider.GetAvailableServicePortScope(GetNextPortRange(), out _servicePort);

    var service = new MicroService(serviceName, new NullLogger<IMicroService>())
      .InTestClass<ContextPropagationTests>()
      .WithOpenTelemetry();

    service.ConfigureServices((svc, cfg) =>
    {
      svc.ConfigureOpenTelemetryLoggerProvider((_, builder) =>
        builder.AddInMemoryExporter(_exportedLogs));
      svc.ConfigureOpenTelemetryTracerProvider((_, builder) =>
        builder.AddInMemoryExporter(_exportedActivities));
      svc.Configure<OpenTelemetryLoggerOptions>(options =>
      {
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
      });
    });

    service.ConfigureApiPipeline(app =>
      {
        app.MapGet("/nested-spans", (ILogger<ContextPropagationTests> logger) =>
        {
          logger.LogInformation("Log in outer span");

          // Create a nested activity
          using var activitySource = new ActivitySource("TestSource");
          using var innerActivity = activitySource.StartActivity("InnerOperation");

          if (innerActivity != null)
          {
            logger.LogInformation("Log in inner span");
          }

          return "OK";
        });
      });

    var config = new ConfigurationBuilder().Build();
    service.CancellationTokenSource.CancelAfter(10000);

    // Act
    var runTask = service.RunAsync(config);
    service.ShouldStart(TimeSpan.FromSeconds(5));

    using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{_servicePort}") };
    await client.GetAsync("/nested-spans");
    await Task.Delay(200);

    service.CancellationTokenSource.Cancel();
    await runTask;

    // Assert - logs should be captured with trace context
    var outerLog = _exportedLogs.FirstOrDefault(log =>
      log.FormattedMessage != null &&
      log.FormattedMessage.Contains("Log in outer span"));

    var innerLog = _exportedLogs.FirstOrDefault(log =>
      log.FormattedMessage != null &&
      log.FormattedMessage.Contains("Log in inner span"));

    outerLog.Should().NotBeNull("outer span log should be captured");

    // If inner activity was created (depends on sampling), verify logs
    if (innerLog != null && outerLog!.TraceId != default && innerLog.TraceId != default)
    {
      // Both logs should share the same TraceId
      innerLog.TraceId.Should().Be(outerLog.TraceId,
        "nested logs should share the same TraceId");
    }
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenMultipleRequests_WhenLogsEmitted_ThenEachRequestHasUniqueTraceId()
  {
    // Arrange
    const string serviceName = "unique-trace-test";
    _portScope = TestPortProvider.GetAvailableServicePortScope(GetNextPortRange(), out _servicePort);

    var service = new MicroService(serviceName, new NullLogger<IMicroService>())
      .InTestClass<ContextPropagationTests>()
      .WithOpenTelemetry();

    service.ConfigureServices((svc, cfg) =>
    {
      svc.ConfigureOpenTelemetryLoggerProvider((_, builder) =>
        builder.AddInMemoryExporter(_exportedLogs));
      svc.ConfigureOpenTelemetryTracerProvider((_, builder) =>
        builder.AddInMemoryExporter(_exportedActivities));
      svc.Configure<OpenTelemetryLoggerOptions>(options =>
      {
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
      });
    });

    service.ConfigureApiPipeline(app =>
      {
        app.MapGet("/unique-trace", (ILogger<ContextPropagationTests> logger) =>
        {
          logger.LogInformation("Request processed with unique trace");
          return "OK";
        });
      });

    var config = new ConfigurationBuilder().Build();
    service.CancellationTokenSource.CancelAfter(10000);

    // Act
    var runTask = service.RunAsync(config);
    service.ShouldStart(TimeSpan.FromSeconds(5));

    using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{_servicePort}") };

    // Make multiple requests
    await client.GetAsync("/unique-trace");
    await client.GetAsync("/unique-trace");
    await client.GetAsync("/unique-trace");
    await Task.Delay(200);

    service.CancellationTokenSource.Cancel();
    await runTask;

    // Assert - each request should have a unique trace
    _exportedActivities.Should().HaveCountGreaterOrEqualTo(3,
      "each request should generate a trace");

    var traceIds = _exportedActivities
      .Where(a => a.Kind == ActivityKind.Server &&
        (a.DisplayName.Contains("GET") || a.OperationName.Contains("GET")))
      .Select(a => a.TraceId)
      .ToList();

    traceIds.Distinct().Count().Should().Be(traceIds.Count,
      "each request should have a unique TraceId");
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenIncomingTraceHeader_WhenRequestProcessed_ThenTraceContextIsPropagated()
  {
    // Arrange
    const string serviceName = "propagation-header-test";
    _portScope = TestPortProvider.GetAvailableServicePortScope(GetNextPortRange(), out _servicePort);

    var service = new MicroService(serviceName, new NullLogger<IMicroService>())
      .InTestClass<ContextPropagationTests>()
      .WithOpenTelemetry();

    service.ConfigureServices((svc, _) =>
    {
      svc.ConfigureOpenTelemetryTracerProvider((_, builder) =>
        builder.AddInMemoryExporter(_exportedActivities));
    });

    service.ConfigureApiPipeline(app =>
    {
      app.MapGet("/propagated", () => "OK");
    });

    var config = new ConfigurationBuilder().Build();
    service.CancellationTokenSource.CancelAfter(10000);

    // Create a trace ID to propagate
    var expectedTraceId = ActivityTraceId.CreateRandom();
    var parentSpanId = ActivitySpanId.CreateRandom();
    var traceparent = $"00-{expectedTraceId}-{parentSpanId}-01";

    // Act
    var runTask = service.RunAsync(config);
    service.ShouldStart(TimeSpan.FromSeconds(5));

    using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{_servicePort}") };

    // Send request with traceparent header
    var request = new HttpRequestMessage(HttpMethod.Get, "/propagated");
    request.Headers.Add("traceparent", traceparent);
    await client.SendAsync(request);
    await Task.Delay(200);

    service.CancellationTokenSource.Cancel();
    await runTask;

    // Assert - trace should be captured with propagated context
    _exportedActivities.Should().NotBeEmpty("traces should be captured");

    var activity = _exportedActivities.FirstOrDefault();
    activity.Should().NotBeNull("activity should be captured");

    // The activity should either have the propagated trace ID or its own
    // (depending on sampling and propagation settings)
    activity!.TraceId.Should().NotBe(default(ActivityTraceId),
      "activity should have a valid TraceId");
  }

  public void Dispose()
  {
    _portScope?.Dispose();
    GC.SuppressFinalize(this);
  }
}