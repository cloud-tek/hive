using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hive.HealthChecks;

/// <summary>
/// Resolves per-check options using the three-tier priority chain:
/// POCO defaults &lt; IConfiguration &lt; Builder (explicit code).
/// Validates that resolved values are within acceptable ranges.
/// </summary>
internal sealed class HealthCheckOptionsResolver
{
  private readonly HealthCheckConfiguration _config;

  public HealthCheckOptionsResolver(HealthCheckConfiguration config)
  {
    _config = config;
  }

  public HiveHealthCheckOptions Resolve(Type checkType)
  {
    HiveHealthCheckOptions options;

    if (_config.ExplicitRegistrations.TryGetValue(checkType, out var explicitOptions))
    {
      options = explicitOptions;
    }
    else
    {
      options = new HiveHealthCheckOptions();
      ReflectionBridge.InvokeConfigureDefaults(checkType, options);
    }

    ApplyConfigurationOverrides(checkType, options);
    Validate(checkType, options);
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

  private static void Validate(Type checkType, HiveHealthCheckOptions options)
  {
    var checkName = ReflectionBridge.GetCheckName(checkType);
    List<string>? errors = null;

    if (options.Interval is { } interval && interval <= TimeSpan.Zero)
      (errors ??= []).Add("Interval must be positive.");

    if (options.FailureThreshold < 1)
      (errors ??= []).Add("FailureThreshold must be at least 1.");

    if (options.SuccessThreshold < 1)
      (errors ??= []).Add("SuccessThreshold must be at least 1.");

    if (options.Timeout <= TimeSpan.Zero)
      (errors ??= []).Add("Timeout must be positive.");

    if (errors is not null)
      throw new OptionsValidationException(checkName, typeof(HiveHealthCheckOptions), errors);
  }
}