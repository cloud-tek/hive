using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Hive.OpenTelemetry;

/// <summary>
/// Extension methods for configuring OpenTelemetry in Hive microservices
/// </summary>
public static class MicroServiceExtensions
{
  /// <summary>
  /// Adds OpenTelemetry support to the microservice with optional configuration overrides
  /// </summary>
  /// <param name="service">The microservice instance</param>
  /// <param name="logging">Optional logging configuration override</param>
  /// <param name="tracing">Optional tracing configuration override</param>
  /// <param name="metrics">Optional metrics configuration override</param>
  /// <returns>The microservice instance for fluent chaining</returns>
  public static IMicroService WithOpenTelemetry(
    this IMicroService service,
    Action<LoggerProviderBuilder>? logging = null,
    Action<TracerProviderBuilder>? tracing = null,
    Action<MeterProviderBuilder>? metrics = null)
  {
    service.Extensions.Add(
      new Extension(
        service: service,
        logging: logging,
        tracing: tracing,
        metrics: metrics));
    return service;
  }
}