namespace Hive.HealthChecks;

/// <summary>
/// Callback builder received inside <c>WithHealthChecks()</c>.
/// Exposes <see cref="WithHealthCheck{T}"/> for registering individual checks.
/// </summary>
public sealed class HealthChecksBuilder
{
  private readonly Dictionary<Type, HiveHealthCheckOptions> _registrations = new();

  /// <summary>
  /// Default evaluation interval for all checks. When set, overrides the value
  /// from IConfiguration. When null, falls back to IConfiguration or the 30s default.
  /// Individual checks can override via <see cref="HiveHealthCheckOptions.Interval"/>.
  /// </summary>
  public TimeSpan? Interval { get; set; }

  /// <summary>
  /// Register a health check with optional per-check configuration.
  /// </summary>
  /// <typeparam name="T">The health check type.</typeparam>
  /// <param name="configure">Optional configuration callback.</param>
  /// <returns>This builder for chaining.</returns>
  public HealthChecksBuilder WithHealthCheck<T>(Action<HiveHealthCheckOptions>? configure = null)
    where T : HiveHealthCheck, IHiveHealthCheck
  {
    var type = typeof(T);
    if (_registrations.ContainsKey(type))
      throw new InvalidOperationException(
        $"Health check '{type.Name}' has already been registered.");

    var options = new HiveHealthCheckOptions();
    ReflectionBridge.InvokeConfigureDefaults(type, options);
    configure?.Invoke(options);
    _registrations[type] = options;
    return this;
  }

  internal IReadOnlyDictionary<Type, HiveHealthCheckOptions> GetRegistrations() => _registrations;
}