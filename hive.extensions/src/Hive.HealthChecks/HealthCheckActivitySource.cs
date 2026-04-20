using System.Diagnostics;

namespace Hive.HealthChecks;

/// <summary>
/// Shared <see cref="ActivitySource"/> for health check evaluation spans.
/// </summary>
internal static class HealthCheckActivitySource
{
  public const string Name = "Hive.HealthChecks";
  public static readonly ActivitySource Source = new(Name);
}