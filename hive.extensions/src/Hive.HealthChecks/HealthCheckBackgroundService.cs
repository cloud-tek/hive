using System.Diagnostics;
using Microsoft.Extensions.Configuration;
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
  private readonly HealthCheckConfiguration _config;
  private readonly HealthCheckStartupGate _gate;
  private readonly ILogger<HealthCheckBackgroundService> _logger;

  public HealthCheckBackgroundService(
    IEnumerable<HiveHealthCheck> checks,
    HealthCheckRegistry registry,
    HealthCheckConfiguration config,
    HealthCheckStartupGate gate,
    ILogger<HealthCheckBackgroundService> logger)
  {
    _checks = checks;
    _registry = registry;
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
      var options = ResolveOptions(check.GetType());
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
    var options = ResolveOptions(checkType);
    var interval = options.Interval ?? _config.GlobalOptions.Interval;

    using var timer = new PeriodicTimer(interval);

    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
      await EvaluateCheck(check, options, stoppingToken);
    }
  }

  private async Task EvaluateCheck(
    HiveHealthCheck check, HiveHealthCheckOptions options, CancellationToken stoppingToken)
  {
    using var activity = HealthCheckActivitySource.Source.StartActivity($"HealthCheck: {check.Name}");
    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
    timeoutCts.CancelAfter(options.Timeout);

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
      // Host is shutting down — don't log, don't update
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

  private HiveHealthCheckOptions ResolveOptions(Type checkType)
  {
    if (_config.ExplicitRegistrations.TryGetValue(checkType, out var explicitOptions))
    {
      ApplyConfigurationOverrides(checkType, explicitOptions);
      return explicitOptions;
    }

    var options = new HiveHealthCheckOptions();
    ReflectionBridge.InvokeConfigureDefaults(checkType, options);
    ApplyConfigurationOverrides(checkType, options);
    return options;
  }

  private void ApplyConfigurationOverrides(Type checkType, HiveHealthCheckOptions options)
  {
    var checkName = ReflectionBridge.GetCheckName(checkType);
    var section = _config.Configuration.GetSection($"{HealthChecksOptions.SectionKey}:Checks:{checkName}");
    if (!section.Exists())
      return;

    if (section[nameof(HiveHealthCheckOptions.Interval)] is { } intervalStr && int.TryParse(intervalStr, out var intervalSecs))
      options.Interval = TimeSpan.FromSeconds(intervalSecs);
    if (section[nameof(HiveHealthCheckOptions.AffectsReadiness)] is { } affectsStr && bool.TryParse(affectsStr, out var affects))
      options.AffectsReadiness = affects;
    if (section[nameof(HiveHealthCheckOptions.BlockReadinessProbeOnStartup)] is { } blockStr && bool.TryParse(blockStr, out var block))
      options.BlockReadinessProbeOnStartup = block;
    if (section[nameof(HiveHealthCheckOptions.ReadinessThreshold)] is { } thresholdStr && Enum.TryParse<ReadinessThreshold>(thresholdStr, true, out var threshold))
      options.ReadinessThreshold = threshold;
    if (section[nameof(HiveHealthCheckOptions.FailureThreshold)] is { } failStr && int.TryParse(failStr, out var fail))
      options.FailureThreshold = fail;
    if (section[nameof(HiveHealthCheckOptions.SuccessThreshold)] is { } successStr && int.TryParse(successStr, out var success))
      options.SuccessThreshold = success;
    if (section[nameof(HiveHealthCheckOptions.Timeout)] is { } timeoutStr && int.TryParse(timeoutStr, out var timeoutSecs))
      options.Timeout = TimeSpan.FromSeconds(timeoutSecs);
  }

  [LoggerMessage(LogLevel.Warning, "Health check '{CheckName}' threw during evaluation")]
  private static partial void LogCheckThrewDuringEvaluation(ILogger logger, string checkName, Exception exception);

  [LoggerMessage(LogLevel.Warning, "Health check '{CheckName}' timed out after {Timeout}")]
  private static partial void LogCheckTimedOut(ILogger logger, string checkName, TimeSpan timeout);
}