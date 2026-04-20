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
  /// <param name="additionalActivitySources">Additional activity source names to subscribe to for tracing</param>
  /// <returns>The function host for fluent chaining</returns>
  public static IFunctionHost WithOpenTelemetry(
    this IFunctionHost functionHost,
    IEnumerable<string>? additionalActivitySources = null)
  {
    var extension = new OpenTelemetry.Extension(functionHost, additionalActivitySources);
    functionHost.Extensions.Add(extension);

    // Extension participates in service configuration
    functionHost.ConfigureServices((services, config) =>
    {
      extension.ConfigureServices(services, functionHost);
    });

    return functionHost;
  }
}