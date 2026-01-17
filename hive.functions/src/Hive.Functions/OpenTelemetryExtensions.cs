using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Hive.Functions;

/// <summary>
/// Extension methods for adding OpenTelemetry to IFunctionHost
/// </summary>
public static class OpenTelemetryExtensions
{
  /// <summary>
  /// Adds OpenTelemetry support to the function host
  /// </summary>
  /// <param name="functionHost">The function host</param>
  /// <param name="logging">Optional logging configuration</param>
  /// <param name="tracing">Optional tracing configuration</param>
  /// <param name="metrics">Optional metrics configuration</param>
  /// <returns>The function host for fluent chaining</returns>
  public static IFunctionHost WithOpenTelemetry(
    this IFunctionHost functionHost,
    Action<LoggerProviderBuilder>? logging = null,
    Action<TracerProviderBuilder>? tracing = null,
    Action<MeterProviderBuilder>? metrics = null)
  {
    var extension = new OpenTelemetry.Extension(functionHost, logging, tracing, metrics);
    functionHost.Extensions.Add(extension);

    // Extension participates in service configuration
    functionHost.ConfigureServices((services, config) =>
    {
      extension.ConfigureServices(services, functionHost);
    });

    return functionHost;
  }
}