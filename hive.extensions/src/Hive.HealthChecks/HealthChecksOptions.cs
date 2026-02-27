namespace Hive.HealthChecks;

/// <summary>
/// Global health check configuration, bound from <c>Hive:HealthChecks</c>.
/// </summary>
internal sealed class HealthChecksOptions
{
  public const string SectionKey = "Hive:HealthChecks";

  /// <summary>
  /// Default evaluation interval for all checks.
  /// Overridden by per-check <see cref="HiveHealthCheckOptions.Interval"/>.
  /// </summary>
  public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
}
