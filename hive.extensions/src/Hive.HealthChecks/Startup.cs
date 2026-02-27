namespace Hive.HealthChecks;

/// <summary>
/// Extension methods for configuring health checks in Hive microservices.
/// </summary>
public static class MicroServiceExtensions
{
  /// <summary>
  /// Adds the Hive health check system to the microservice.
  /// </summary>
  /// <param name="service">The microservice instance.</param>
  /// <param name="configure">Optional callback to configure health checks.</param>
  /// <returns>The microservice instance for fluent chaining.</returns>
  public static IMicroService WithHealthChecks(
    this IMicroService service, Action<HealthChecksBuilder>? configure = null)
  {
    if (service.Extensions.Any(e => e is HealthChecksExtension))
      throw new InvalidOperationException("WithHealthChecks() has already been called.");

    var builder = new HealthChecksBuilder();
    configure?.Invoke(builder);
    service.Extensions.Add(new HealthChecksExtension(service, builder));
    return service;
  }
}
