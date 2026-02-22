namespace Hive.HTTP.Configuration;

public class SocketsHandlerOptions
{
  public TimeSpan PooledConnectionLifetime { get; set; } = TimeSpan.FromMinutes(2);

  public TimeSpan PooledConnectionIdleTimeout { get; set; } = TimeSpan.FromMinutes(1);

  public int MaxConnectionsPerServer { get; set; } = 100;
}