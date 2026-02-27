namespace Hive.HealthChecks;

/// <summary>
/// Interface for Hive health checks. Implementations are
/// auto-discovered via Scrutor assembly scanning when
/// <c>.WithHealthChecks()</c> is registered.
/// <para>
/// Uses C# 11 static abstract members so that <see cref="ConfigureDefaults"/>
/// can be called during ConfigureServices (before DI container is built)
/// without needing an instance of the health check.
/// </para>
/// </summary>
public interface IHiveHealthCheck
{
  /// <summary>
  /// Instance-level name, used at runtime for probes, logging, and display.
  /// The <c>HiveHealthCheck</c> base class provides a default implementation
  /// that delegates to <see cref="CheckName"/> via a reflection bridge.
  /// </summary>
  string Name { get; }

  /// <summary>
  /// Static name used during ConfigureServices (before DI container exists)
  /// for IConfiguration section lookup: <c>Hive:HealthChecks:Checks:{CheckName}:...</c>
  /// and for TOptions convention binding.
  /// </summary>
  static abstract string CheckName { get; }

  /// <summary>
  /// Provide check-specific default options (e.g., AffectsReadiness, Interval).
  /// Called as <c>T.ConfigureDefaults(options)</c> during service registration --
  /// no instance required. IConfiguration and fluent API overrides
  /// take precedence over these defaults.
  /// </summary>
  static abstract void ConfigureDefaults(HiveHealthCheckOptions options);
}
