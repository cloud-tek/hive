using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hive.Logging.Services;

internal class LoggingService : IHostedService
{
  public LoggingService(IOptions<Options> options)
  {
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }
}