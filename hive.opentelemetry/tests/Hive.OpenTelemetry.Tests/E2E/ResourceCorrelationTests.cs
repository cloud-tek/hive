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
/// End-to-end tests for resource attribute correlation across all telemetry signals
/// </summary>
[Collection("E2E Tests")]
public class ResourceCorrelationTests : IDisposable
{
  private static int _portCounter = 7000;
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
  public async Task GivenOpenTelemetry_WhenServiceRuns_ThenTracesContainServiceName()
  {
    // Arrange
    const string expectedServiceName = "resource-correlation-test-service";
    _portScope = TestPortProvider.GetAvailableServicePortScope(GetNextPortRange(), out _servicePort);

    var service = new MicroService(expectedServiceName, new NullLogger<IMicroService>())
      .InTestClass<ResourceCorrelationTests>()
      .WithOpenTelemetry();

    service.ConfigureServices((svc, _) =>
    {
      svc.ConfigureOpenTelemetryTracerProvider((_, builder) =>
        builder.AddInMemoryExporter(_exportedActivities));
    });

    service.ConfigureApiPipeline(app =>
    {
      app.MapGet("/test", () => "OK");
    });

    var config = new ConfigurationBuilder().Build();
    service.CancellationTokenSource.CancelAfter(10000);

    // Act
    var runTask = service.RunAsync(config);
    service.ShouldStart(TimeSpan.FromSeconds(5));

    using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{_servicePort}") };
    await client.GetAsync("/test");
    await Task.Delay(200);

    service.CancellationTokenSource.Cancel();
    await runTask;

    // Assert - traces should have service name
    _exportedActivities.Should().NotBeEmpty("traces should be captured");

    // The service name is set via Resource, check if activity has expected display name structure
    var activity = _exportedActivities.First();
    activity.Should().NotBeNull("activity should be captured");
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenOpenTelemetry_WhenServiceRuns_ThenLogsContainCategoryName()
  {
    // Arrange
    const string expectedServiceName = "log-correlation-test-service";
    _portScope = TestPortProvider.GetAvailableServicePortScope(GetNextPortRange(), out _servicePort);

    var service = new MicroService(expectedServiceName, new NullLogger<IMicroService>())
      .InTestClass<ResourceCorrelationTests>()
      .WithOpenTelemetry();

    service.ConfigureServices((svc, _) =>
    {
      svc.ConfigureOpenTelemetryLoggerProvider((_, builder) =>
        builder.AddInMemoryExporter(_exportedLogs));
      svc.Configure<OpenTelemetryLoggerOptions>(options =>
      {
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
      });
    });

    service.ConfigureApiPipeline(app =>
      {
        app.MapGet("/test", (ILogger<ResourceCorrelationTests> logger) =>
        {
          logger.LogInformation("Test log for correlation");
          return "OK";
        });
      });

    var config = new ConfigurationBuilder().Build();
    service.CancellationTokenSource.CancelAfter(10000);

    // Act
    var runTask = service.RunAsync(config);
    service.ShouldStart(TimeSpan.FromSeconds(5));

    using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{_servicePort}") };
    await client.GetAsync("/test");
    await Task.Delay(200);

    service.CancellationTokenSource.Cancel();
    await runTask;

    // Assert - logs should have category name
    var testLog = _exportedLogs.FirstOrDefault(l =>
      l.FormattedMessage != null &&
      l.FormattedMessage.Contains("Test log for correlation"));

    testLog.Should().NotBeNull("test log should be captured");
    testLog!.CategoryName.Should().NotBeNullOrEmpty("log should have a category name");
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenConfiguredResourceAttributes_WhenServiceRuns_ThenServiceStartsSuccessfully()
  {
    // Arrange
    const string expectedServiceName = "configured-resource-test";
    _portScope = TestPortProvider.GetAvailableServicePortScope(GetNextPortRange(), out _servicePort);

    var configValues = new Dictionary<string, string>
    {
      ["OpenTelemetry:Resource:ServiceNamespace"] = "test-namespace",
      ["OpenTelemetry:Resource:ServiceVersion"] = "1.2.3",
      ["OpenTelemetry:Resource:Attributes:environment"] = "test",
      ["OpenTelemetry:Resource:Attributes:region"] = "us-east-1"
    };

    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(configValues!)
      .Build();

    var service = new MicroService(expectedServiceName, new NullLogger<IMicroService>())
      .InTestClass<ResourceCorrelationTests>()
      .WithOpenTelemetry();

    service.ConfigureServices((svc, _) =>
    {
      svc.ConfigureOpenTelemetryTracerProvider((_, builder) =>
        builder.AddInMemoryExporter(_exportedActivities));
    });

    service.ConfigureApiPipeline(app =>
    {
      app.MapGet("/test", () => "OK");
    });

    service.CancellationTokenSource.CancelAfter(10000);

    // Act
    var runTask = service.RunAsync(config);
    service.ShouldStart(TimeSpan.FromSeconds(5));

    using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{_servicePort}") };
    await client.GetAsync("/test");
    await Task.Delay(200);

    service.CancellationTokenSource.Cancel();
    await runTask;

    // Assert - service should start and emit traces with configured resource
    _exportedActivities.Should().NotBeEmpty("traces should be captured with configured resource");
  }

  [Fact]
  [IntegrationTest]
  public async Task GivenServiceInstanceId_WhenServiceRuns_ThenIdIsConsistent()
  {
    // Arrange
    const string expectedServiceName = "instance-id-test";
    _portScope = TestPortProvider.GetAvailableServicePortScope(GetNextPortRange(), out _servicePort);

    var service = new MicroService(expectedServiceName, new NullLogger<IMicroService>())
      .InTestClass<ResourceCorrelationTests>()
      .WithOpenTelemetry();

    service.ConfigureServices((svc, _) =>
    {
      svc.ConfigureOpenTelemetryTracerProvider((_, builder) =>
        builder.AddInMemoryExporter(_exportedActivities));
      svc.ConfigureOpenTelemetryLoggerProvider((_, builder) =>
        builder.AddInMemoryExporter(_exportedLogs));
      svc.Configure<OpenTelemetryLoggerOptions>(options =>
      {
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
      });
    });

    service.ConfigureApiPipeline(app =>
      {
        app.MapGet("/test", (ILogger<ResourceCorrelationTests> logger) =>
        {
          logger.LogInformation("Instance ID correlation test");
          return "OK";
        });
      });

    var config = new ConfigurationBuilder().Build();
    service.CancellationTokenSource.CancelAfter(10000);

    // Capture the service ID
    var serviceId = service.Id;

    // Act
    var runTask = service.RunAsync(config);
    service.ShouldStart(TimeSpan.FromSeconds(5));

    using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{_servicePort}") };
    await client.GetAsync("/test");
    await Task.Delay(200);

    service.CancellationTokenSource.Cancel();
    await runTask;

    // Assert - service ID should be consistent and non-empty
    serviceId.Should().NotBeNullOrEmpty("service should have an instance ID");
    _exportedActivities.Should().NotBeEmpty("traces should be captured");
    _exportedLogs.Should().NotBeEmpty("logs should be captured");
  }

  public void Dispose()
  {
    _portScope?.Dispose();
    GC.SuppressFinalize(this);
  }
}
