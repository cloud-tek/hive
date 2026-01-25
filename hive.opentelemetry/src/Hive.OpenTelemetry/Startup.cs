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
  /// Adds OpenTelemetry support to any Hive service host with optional configuration overrides
  /// </summary>
  /// <typeparam name="THost">The type of service host (IMicroService, IFunctionHost, etc.)</typeparam>
  /// <param name="service">The service host instance</param>
  /// <param name="logging">Optional logging configuration override</param>
  /// <param name="tracing">Optional tracing configuration override</param>
  /// <param name="metrics">Optional metrics configuration override</param>
  /// <returns>The service host instance for fluent chaining</returns>
  public static THost WithOpenTelemetry<THost>(
    this THost service,
    Action<LoggerProviderBuilder>? logging = null,
    Action<TracerProviderBuilder>? tracing = null,
    Action<MeterProviderBuilder>? metrics = null)
    where THost : IMicroServiceCore
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