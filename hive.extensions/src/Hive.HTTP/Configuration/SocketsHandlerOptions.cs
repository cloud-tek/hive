namespace Hive.HTTP.Configuration;

public class SocketsHandlerOptions
{
  public TimeSpan PooledConnectionLifetime { get; set; } = Timeout.InfiniteTimeSpan;

  public TimeSpan PooledConnectionIdleTimeout { get; set; } = TimeSpan.FromMinutes(1);

  public int MaxConnectionsPerServer { get; set; } = int.MaxValue;
}