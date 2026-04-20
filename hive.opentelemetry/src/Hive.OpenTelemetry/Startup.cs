namespace Hive.OpenTelemetry;

/// <summary>
/// Extension methods for configuring OpenTelemetry in Hive microservices
/// </summary>
public static class MicroServiceExtensions
{
  /// <summary>
  /// Adds OpenTelemetry support to any Hive service host
  /// </summary>
  /// <typeparam name="THost">The type of service host (IMicroService, IFunctionHost, etc.)</typeparam>
  /// <param name="service">The service host instance</param>
  /// <param name="additionalActivitySources">Additional activity source names to subscribe to for tracing</param>
  /// <returns>The service host instance for fluent chaining</returns>
  public static THost WithOpenTelemetry<THost>(
    this THost service,
    IEnumerable<string>? additionalActivitySources = null)
    where THost : IMicroServiceCore
  {
    service.Extensions.Add(
      new Extension(
        service: service,
        additionalActivitySources: additionalActivitySources));
    return service;
  }
}