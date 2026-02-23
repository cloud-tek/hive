namespace Hive.HTTP.Configuration;

/// <summary>
/// Configuration options for <see cref="System.Net.Http.SocketsHttpHandler"/> connection pooling.
/// All defaults match the .NET framework defaults.
/// </summary>
public class SocketsHandlerOptions
{
  /// <summary>
  /// How long a connection can live in the pool before being recycled.
  /// </summary>
  public TimeSpan PooledConnectionLifetime { get; set; } = Timeout.InfiniteTimeSpan;

  /// <summary>
  /// How long an idle connection remains in the pool before being removed.
  /// </summary>
  public TimeSpan PooledConnectionIdleTimeout { get; set; } = TimeSpan.FromMinutes(1);

  /// <summary>
  /// Maximum number of concurrent connections allowed per server.
  /// </summary>
  public int MaxConnectionsPerServer { get; set; } = int.MaxValue;
}