using Hive.HealthChecks;
using Hive.Messaging.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace Hive.Messaging.RabbitMq.HealthChecks;

/// <summary>
/// Hive health check for RabbitMQ connectivity. Wraps the upstream
/// <c>HealthChecks.RabbitMQ.RabbitMQHealthCheck</c> and maps results
/// to <see cref="HealthCheckStatus"/>.
/// </summary>
public sealed class RabbitMqHealthCheck : HiveHealthCheck, IHiveHealthCheck
{
  private readonly global::HealthChecks.RabbitMQ.RabbitMQHealthCheck _inner;

  /// <inheritdoc />
  public static string CheckName => "RabbitMq";

  /// <inheritdoc />
  public static void ConfigureDefaults(HiveHealthCheckOptions options)
  {
    options.AffectsReadiness = true;
    options.BlockReadinessProbeOnStartup = true;
  }

  /// <summary>
  /// Creates a new <see cref="RabbitMqHealthCheck"/> instance.
  /// The connection URI is read from <c>Hive:Messaging:RabbitMq:ConnectionUri</c>.
  /// </summary>
  public RabbitMqHealthCheck(IServiceProvider serviceProvider, IConfiguration configuration)
  {
    var connectionUri = configuration[$"{MessagingOptions.SectionKey}:RabbitMq:ConnectionUri"]
      ?? throw new InvalidOperationException(
        $"RabbitMq health check requires '{MessagingOptions.SectionKey}:RabbitMq:ConnectionUri' to be configured.");

    IConnection? cachedConnection = null;

    _inner = new global::HealthChecks.RabbitMQ.RabbitMQHealthCheck(serviceProvider, async _ =>
    {
      if (cachedConnection is { IsOpen: true })
        return cachedConnection;

      var factory = new ConnectionFactory { Uri = new Uri(connectionUri) };
      cachedConnection = await factory.CreateConnectionAsync();
      return cachedConnection;
    });
  }

  /// <inheritdoc />
  public override async Task<HealthCheckStatus> EvaluateAsync(CancellationToken ct)
  {
    var result = await _inner.CheckHealthAsync(
      new HealthCheckContext
      {
        Registration = new HealthCheckRegistration(
          CheckName, _inner, HealthStatus.Unhealthy, null)
      },
      ct);

    return result.Status == HealthStatus.Healthy
      ? HealthCheckStatus.Healthy
      : HealthCheckStatus.Unhealthy;
  }
}