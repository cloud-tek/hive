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
  private readonly HealthCheckOptionsResolver _resolver;
  private readonly HealthCheckConfiguration _config;
  private readonly HealthCheckStartupGate _gate;

  public HealthCheckStartupService(
    ILoggerFactory loggerFactory,
    IEnumerable<HiveHealthCheck> checks,
    HealthCheckRegistry registry,
    HealthCheckOptionsResolver resolver,
    HealthCheckConfiguration config,
    HealthCheckStartupGate gate)
    : base(loggerFactory)
  {
    _checks = checks;
    _registry = registry;
    _resolver = resolver;
    _config = config;
    _gate = gate;
  }

  protected override async Task OnStartAsync(CancellationToken cancellationToken)
  {
    try
    {
      // Initialize all checks in the registry and bind TOptions
      foreach (var check in _checks)
      {
        var checkType = check.GetType();
        var options = _resolver.Resolve(checkType);
        _registry.Register(check.Name, options);
        BindCheckOptions(check, checkType);
      }

      // Evaluate blocking checks sequentially
      foreach (var check in _checks)
      {
        var checkType = check.GetType();
        var options = _resolver.Resolve(checkType);

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
    finally
    {
      _gate.Signal();
    }
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
          var optionsInstance = Activator.CreateInstance(optionsType)
            ?? throw new InvalidOperationException(
              $"Failed to create options instance for health check '{checkName}' (type: {optionsType.FullName}).");
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