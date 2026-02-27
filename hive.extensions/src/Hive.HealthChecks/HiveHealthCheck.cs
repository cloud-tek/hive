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
