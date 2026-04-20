using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hive.HealthChecks;

/// <summary>
/// Background service that runs independent <see cref="PeriodicTimer"/> loops
/// for each registered health check. Started after the startup phase completes.
/// </summary>
internal sealed partial class HealthCheckBackgroundService : BackgroundService
{
  private readonly IEnumerable<HiveHealthCheck> _checks;
  private readonly HealthCheckRegistry _registry;
  private readonly HealthCheckOptionsResolver _resolver;
  private readonly HealthCheckConfiguration _config;
  private readonly HealthCheckStartupGate _gate;
  private readonly ILogger<HealthCheckBackgroundService> _logger;

  public HealthCheckBackgroundService(
    IEnumerable<HiveHealthCheck> checks,
    HealthCheckRegistry registry,
    HealthCheckOptionsResolver resolver,
    HealthCheckConfiguration config,
    HealthCheckStartupGate gate,
    ILogger<HealthCheckBackgroundService> logger)
  {
    _checks = checks;
    _registry = registry;
    _resolver = resolver;
    _config = config;
    _gate = gate;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    // Wait for startup service to complete registration and blocking evaluation
    await _gate.WaitAsync(stoppingToken);

    // Evaluate all checks once eagerly so no check remains Unknown
    var initialTasks = _checks.Select(check =>
    {
      var options = _resolver.Resolve(check.GetType());
      return EvaluateCheck(check, options, stoppingToken);
    });
    await Task.WhenAll(initialTasks);

    // Start independent periodic timer loops
    var loopTasks = _checks.Select(check => RunCheckLoop(check, stoppingToken));
    await Task.WhenAll(loopTasks);
  }

  private async Task RunCheckLoop(HiveHealthCheck check, CancellationToken stoppingToken)
  {
    var checkType = check.GetType();
    var options = _resolver.Resolve(checkType);
    var interval = options.Interval ?? _config.GlobalOptions.Interval;

    using var timer = new PeriodicTimer(interval);
    var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

    try
    {
      while (await timer.WaitForNextTickAsync(stoppingToken))
      {
        timeoutCts.CancelAfter(options.Timeout);
        await EvaluateCheckCore(check, options, timeoutCts, stoppingToken);

        if (!timeoutCts.TryReset())
        {
          timeoutCts.Dispose();
          timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        }
      }
    }
    finally
    {
      timeoutCts.Dispose();
    }
  }

  private async Task EvaluateCheck(
    HiveHealthCheck check, HiveHealthCheckOptions options, CancellationToken stoppingToken)
  {
    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
    timeoutCts.CancelAfter(options.Timeout);
    await EvaluateCheckCore(check, options, timeoutCts, stoppingToken);
  }

  private async Task EvaluateCheckCore(
    HiveHealthCheck check, HiveHealthCheckOptions options,
    CancellationTokenSource timeoutCts, CancellationToken stoppingToken)
  {
    using var activity = HealthCheckActivitySource.Source.StartActivity($"HealthCheck: {check.Name}");

    var sw = Stopwatch.StartNew();
    try
    {
      var status = await check.EvaluateAsync(timeoutCts.Token);
      sw.Stop();
      _registry.UpdateAndRecompute(check.Name, status, sw.Elapsed, null);
      activity?.SetTag("healthcheck.status", status.ToString());
    }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
    {
      // Host is shutting down — graceful cancellation, no action needed
      return;
    }
    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
    {
      sw.Stop();
      var error = $"Evaluation timed out after {options.Timeout.TotalSeconds}s";
      _registry.UpdateAndRecompute(check.Name, HealthCheckStatus.Unhealthy, sw.Elapsed, error);
      activity?.SetTag("healthcheck.status", HealthCheckStatus.Unhealthy.ToString());
      activity?.SetStatus(ActivityStatusCode.Error, error);
      LogCheckTimedOut(_logger, check.Name, options.Timeout);
    }
    catch (Exception ex)
    {
      sw.Stop();
      _registry.UpdateAndRecompute(check.Name, HealthCheckStatus.Unhealthy, sw.Elapsed, ex.Message);
      activity?.SetTag("healthcheck.status", HealthCheckStatus.Unhealthy.ToString());
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      LogCheckThrewDuringEvaluation(_logger, check.Name, ex);
    }
  }

  [LoggerMessage(LogLevel.Warning, "Health check '{CheckName}' threw during evaluation")]
  private static partial void LogCheckThrewDuringEvaluation(ILogger logger, string checkName, Exception exception);

  [LoggerMessage(LogLevel.Warning, "Health check '{CheckName}' timed out after {Timeout}")]
  private static partial void LogCheckTimedOut(ILogger logger, string checkName, TimeSpan timeout);
}