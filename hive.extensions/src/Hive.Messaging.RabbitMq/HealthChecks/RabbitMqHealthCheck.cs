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
public sealed class RabbitMqHealthCheck : HiveHealthCheck, IHiveHealthCheck, IAsyncDisposable
{
  private readonly global::HealthChecks.RabbitMQ.RabbitMQHealthCheck _inner;
  private readonly SemaphoreSlim _connectionLock = new(1, 1);
  private IConnection? _cachedConnection;

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

    _inner = new global::HealthChecks.RabbitMQ.RabbitMQHealthCheck(serviceProvider, async _ =>
    {
      await _connectionLock.WaitAsync();
      try
      {
        if (_cachedConnection is { IsOpen: true })
          return _cachedConnection;

        if (_cachedConnection is not null)
          await _cachedConnection.DisposeAsync();

        var factory = new ConnectionFactory { Uri = new Uri(connectionUri) };
        _cachedConnection = await factory.CreateConnectionAsync();
        return _cachedConnection;
      }
      finally
      {
        _connectionLock.Release();
      }
    });
  }

  /// <inheritdoc />
  public async ValueTask DisposeAsync()
  {
    await _connectionLock.WaitAsync();
    try
    {
      if (_cachedConnection is not null)
        await _cachedConnection.DisposeAsync();
    }
    finally
    {
      _connectionLock.Dispose();
    }
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

    return result.Status switch
    {
      HealthStatus.Healthy => HealthCheckStatus.Healthy,
      HealthStatus.Degraded => HealthCheckStatus.Degraded,
      _ => HealthCheckStatus.Unhealthy
    };
  }
}