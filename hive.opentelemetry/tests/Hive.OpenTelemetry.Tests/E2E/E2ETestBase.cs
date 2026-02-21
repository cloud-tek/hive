using System.Diagnostics;
using System.Net.Http;
using Hive.MicroServices;
using Hive.MicroServices.Api;
using Hive.MicroServices.Extensions;
using Hive.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Hive.OpenTelemetry.Tests.E2E;

/// <summary>
/// Base class for E2E observability tests providing common infrastructure
/// for capturing and asserting on telemetry signals.
/// </summary>
public abstract class E2ETestBase : IDisposable
{
  private static int _portCounter = 6000;

  /// <summary>
  /// Collection for captured activities (traces)
  /// </summary>
  protected List<Activity> ExportedActivities { get; } = new();

  /// <summary>
  /// Collection for captured metrics
  /// </summary>
  protected List<Metric> ExportedMetrics { get; } = new();

  /// <summary>
  /// Collection for captured log records
  /// </summary>
  protected List<LogRecord> ExportedLogs { get; } = new();

  /// <summary>
  /// The port assigned to the test service
  /// </summary>
  protected ushort ServicePort { get; private set; }

  /// <summary>
  /// Environment variable scope for port configuration
  /// </summary>
  private IDisposable? _portScope;

  /// <summary>
  /// Gets a unique starting port for this test instance
  /// </summary>
  private static ushort GetNextPortRange()
  {
    return (ushort)Interlocked.Add(ref _portCounter, 10);
  }

  /// <summary>
  /// Creates a configured MicroService with in-memory exporters for all signals
  /// </summary>
  /// <typeparam name="TTestClass">The test class type for isolation</typeparam>
  /// <param name="serviceName">Name of the test service</param>
  /// <param name="configureEndpoints">Optional endpoint configuration</param>
  /// <param name="configureServices">Optional service configuration</param>
  /// <returns>Configured IMicroService instance</returns>
  protected IMicroService CreateTestService<TTestClass>(
    string serviceName,
    Action<IEndpointRouteBuilder>? configureEndpoints = null,
    Action<IServiceCollection, IConfiguration>? configureServices = null)
    where TTestClass : class
  {
    // Get available port and set environment variable
    _portScope = TestPortProvider.GetAvailableServicePortScope(GetNextPortRange(), out var port);
    ServicePort = port;

    var service = new MicroService(serviceName, new NullLogger<IMicroService>())
      .InTestClass<TTestClass>()
      .WithOpenTelemetry(
        logging: builder => ConfigureLoggingExporter(builder),
        tracing: builder => ConfigureTracingExporter(builder),
        metrics: builder => ConfigureMetricsExporter(builder));

    // Configure OpenTelemetryLoggerOptions to include formatted messages for testing
    service.ConfigureServices((svc, cfg) =>
    {
      svc.Configure<OpenTelemetryLoggerOptions>(options =>
      {
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
      });
    });

    if (configureServices != null)
    {
      service.ConfigureServices(configureServices);
    }

    service.ConfigureApiPipeline(app =>
    {
      configureEndpoints?.Invoke(app);
    });

    return service;
  }

  /// <summary>
  /// Creates a configured MicroService with only tracing in-memory exporter
  /// </summary>
  protected IMicroService CreateTracingTestService<TTestClass>(
    string serviceName,
    Action<IEndpointRouteBuilder>? configureEndpoints = null)
    where TTestClass : class
  {
    _portScope = TestPortProvider.GetAvailableServicePortScope(GetNextPortRange(), out var port);
    ServicePort = port;

    var service = new MicroService(serviceName, new NullLogger<IMicroService>())
      .InTestClass<TTestClass>()
      .WithOpenTelemetry(
        tracing: builder => ConfigureTracingExporter(builder));

    service.ConfigureApiPipeline(app =>
    {
      configureEndpoints?.Invoke(app);
    });

    return service;
  }

  /// <summary>
  /// Creates a configured MicroService with only metrics in-memory exporter
  /// </summary>
  protected IMicroService CreateMetricsTestService<TTestClass>(
    string serviceName,
    Action<IEndpointRouteBuilder>? configureEndpoints = null)
    where TTestClass : class
  {
    _portScope = TestPortProvider.GetAvailableServicePortScope(GetNextPortRange(), out var port);
    ServicePort = port;

    var service = new MicroService(serviceName, new NullLogger<IMicroService>())
      .InTestClass<TTestClass>()
      .WithOpenTelemetry(
        metrics: builder => ConfigureMetricsExporter(builder));

    service.ConfigureApiPipeline(app =>
    {
      configureEndpoints?.Invoke(app);
    });

    return service;
  }

  /// <summary>
  /// Creates a configured MicroService with only logging in-memory exporter
  /// </summary>
  protected IMicroService CreateLoggingTestService<TTestClass>(
    string serviceName,
    Action<IEndpointRouteBuilder>? configureEndpoints = null,
    Action<IServiceCollection, IConfiguration>? configureServices = null)
    where TTestClass : class
  {
    _portScope = TestPortProvider.GetAvailableServicePortScope(GetNextPortRange(), out var port);
    ServicePort = port;

    var service = new MicroService(serviceName, new NullLogger<IMicroService>())
      .InTestClass<TTestClass>()
      .WithOpenTelemetry(
        logging: builder => ConfigureLoggingExporter(builder));

    // Configure OpenTelemetryLoggerOptions to include formatted messages for testing
    service.ConfigureServices((svc, cfg) =>
    {
      svc.Configure<OpenTelemetryLoggerOptions>(options =>
      {
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
      });
    });

    if (configureServices != null)
    {
      service.ConfigureServices(configureServices);
    }

    service.ConfigureApiPipeline(app =>
    {
      configureEndpoints?.Invoke(app);
    });

    return service;
  }

  /// <summary>
  /// Creates an HttpClient configured to communicate with the test service
  /// </summary>
  protected HttpClient CreateHttpClient()
  {
    return new HttpClient
    {
      BaseAddress = new Uri($"http://localhost:{ServicePort}")
    };
  }

  /// <summary>
  /// Runs the service, executes the test action, then shuts down
  /// </summary>
  protected async Task RunServiceAndExecuteAsync(
    IMicroService service,
    Func<Task> testAction,
    int startupDelayMs = 500,
    int shutdownDelayMs = 100)
  {
    service.CancellationTokenSource.CancelAfter(15000); // Safety timeout

    var runTask = service.RunAsync(new ConfigurationBuilder().Build());

    // Wait for service to start
    service.ShouldStart(TimeSpan.FromSeconds(10));

    try
    {
      // Execute test action
      await testAction();

      // Brief delay to allow telemetry to be exported
      await Task.Delay(shutdownDelayMs);
    }
    finally
    {
      // Shutdown service
      service.CancellationTokenSource.Cancel();
      await runTask;
    }
  }

  private void ConfigureLoggingExporter(LoggerProviderBuilder builder)
  {
    builder.AddInMemoryExporter(ExportedLogs);
  }

  private void ConfigureTracingExporter(TracerProviderBuilder builder)
  {
    builder.AddAspNetCoreInstrumentation();
    builder.AddHttpClientInstrumentation();
    builder.AddInMemoryExporter(ExportedActivities);
  }

  private void ConfigureMetricsExporter(MeterProviderBuilder builder)
  {
    builder.AddAspNetCoreInstrumentation();
    builder.AddHttpClientInstrumentation();
    builder.AddRuntimeInstrumentation();
    builder.AddInMemoryExporter(ExportedMetrics);
  }

  public void Dispose()
  {
    _portScope?.Dispose();
    GC.SuppressFinalize(this);
  }
}