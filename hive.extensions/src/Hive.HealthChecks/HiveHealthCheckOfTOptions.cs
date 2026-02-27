namespace Hive.HealthChecks;

/// <summary>
/// Options-aware variant of <see cref="HiveHealthCheck"/>. The framework binds
/// <typeparamref name="TOptions"/> from IConfiguration by convention:
/// <c>Hive:HealthChecks:Checks:{CheckName}:Options</c>.
/// </summary>
/// <typeparam name="TOptions">Check-specific options type.</typeparam>
public abstract class HiveHealthCheck<TOptions> : HiveHealthCheck
  where TOptions : class, new()
{
  /// <summary>
  /// Check-specific options, bound from IConfiguration by convention.
  /// Set by the framework before evaluation begins.
  /// </summary>
  public TOptions Options { get; internal set; } = new();
}
