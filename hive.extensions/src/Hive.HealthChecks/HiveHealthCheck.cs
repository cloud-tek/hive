namespace Hive.HealthChecks;

/// <summary>
/// Abstract base class for Hive health checks. Extension authors inherit from this class
/// (or <see cref="HiveHealthCheck{TOptions}"/>) to create specialized health checks.
/// </summary>
public abstract class HiveHealthCheck
{
  /// <summary>
  /// Instance-level name. Defaults to delegating to the static <c>CheckName</c>
  /// via a reflection bridge. Concrete subclasses only need to implement the static
  /// <c>CheckName</c> property.
  /// </summary>
  public virtual string Name => ReflectionBridge.GetCheckName(GetType());

  /// <summary>
  /// Evaluate the health of the dependency. Dependencies are
  /// constructor-injected (health checks are DI singletons).
  /// For scoped services (e.g., DbContext), inject <c>IServiceScopeFactory</c>.
  /// </summary>
  public abstract Task<HealthCheckStatus> EvaluateAsync(CancellationToken ct);
}

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