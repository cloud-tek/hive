using System.Diagnostics;
using Hive.MicroServices.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hive.HealthChecks;

/// <summary>
/// Blocking startup service that evaluates health checks with
/// <c>BlockReadinessProbeOnStartup = true</c> before the service
/// reports readiness. Unhealthy results cause startup failure.
/// </summary>
internal sealed partial class HealthCheckStartupService : HostedStartupService<HealthCheckStartupService>
{
  private readonly IEnumerable<HiveHealthCheck> _checks;
  private readonly HealthCheckRegistry _registry;
  private readonly HealthCheckConfiguration _config;

  public HealthCheckStartupService(
    ILoggerFactory loggerFactory,
    IEnumerable<HiveHealthCheck> checks,
    HealthCheckRegistry registry,
    HealthCheckConfiguration config)
    : base(loggerFactory)
  {
    _checks = checks;
    _registry = registry;
    _config = config;
  }

  protected override async Task OnStartAsync(CancellationToken cancellationToken)
  {
    // Initialize all checks in the registry and bind TOptions
    foreach (var check in _checks)
    {
      var checkType = check.GetType();
      var options = ResolveOptions(checkType);
      _registry.Register(check.Name, options);
      BindCheckOptions(check, checkType);
    }

    // Evaluate blocking checks sequentially
    foreach (var check in _checks)
    {
      var checkType = check.GetType();
      var options = ResolveOptions(checkType);

      if (!options.BlockReadinessProbeOnStartup)
        continue;

      using var activity = HealthCheckActivitySource.Source.StartActivity($"HealthCheck: {check.Name}");
      var sw = Stopwatch.StartNew();
      try
      {
        var status = await check.EvaluateAsync(cancellationToken);
        sw.Stop();
        _registry.UpdateAndRecompute(check.Name, status, sw.Elapsed, null);
        activity?.SetTag("healthcheck.status", status.ToString());

        if (status == HealthCheckStatus.Unhealthy)
        {
          activity?.SetStatus(ActivityStatusCode.Error, "Unhealthy during startup");
          throw new InvalidOperationException(
            $"Health check '{check.Name}' returned Unhealthy during startup.");
        }
      }
      catch (InvalidOperationException)
      {
        throw; // Re-throw our own startup failure
      }
      catch (Exception ex)
      {
        sw.Stop();
        _registry.UpdateAndRecompute(check.Name, HealthCheckStatus.Unhealthy, sw.Elapsed, ex.Message);
        activity?.SetTag("healthcheck.status", HealthCheckStatus.Unhealthy.ToString());
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        LogCheckThrewDuringStartup(Logger, check.Name, ex);
        throw new InvalidOperationException(
          $"Health check '{check.Name}' failed during startup.", ex);
      }
    }
  }

  private HiveHealthCheckOptions ResolveOptions(Type checkType)
  {
    // Priority: explicit registration > IConfiguration > ConfigureDefaults > global defaults
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

  private void BindCheckOptions(HiveHealthCheck check, Type checkType)
  {
    // Detect HiveHealthCheck<TOptions> and bind TOptions from IConfiguration
    var baseType = checkType.BaseType;
    while (baseType != null)
    {
      if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(HiveHealthCheck<>))
      {
        var optionsType = baseType.GetGenericArguments()[0];
        var checkName = ReflectionBridge.GetCheckName(checkType);
        var section = _config.Configuration.GetSection(
          $"{HealthChecksOptions.SectionKey}:Checks:{checkName}:Options");

        if (section.Exists())
        {
          var optionsInstance = Activator.CreateInstance(optionsType)!;
          Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(section, optionsInstance);

          var optionsProp = baseType.GetProperty(nameof(HiveHealthCheck<object>.Options))!;
          optionsProp.SetValue(check, optionsInstance);
        }

        break;
      }
      baseType = baseType.BaseType;
    }
  }

  [LoggerMessage(LogLevel.Warning, "Health check '{CheckName}' threw during startup evaluation")]
  private static partial void LogCheckThrewDuringStartup(ILogger logger, string checkName, Exception exception);
}
