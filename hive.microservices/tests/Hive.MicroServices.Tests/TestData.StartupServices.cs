using FluentAssertions.Extensions;
using Hive.MicroServices.Lifecycle;

namespace Hive.MicroServices.Tests;

internal static class TestData
{
  // ReSharper disable once ClassNeverInstantiated.Global
  internal sealed class Sec2DelayStartupService : IHostedStartupService
  {
    public bool Completed { get; set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
      await Task.Delay(2.Seconds(), cancellationToken);

      Completed = true;
    }
  }

  // ReSharper disable once ClassNeverInstantiated.Global
  internal sealed class FailingSec2DelayStartupService : IHostedStartupService
  {
    public bool Completed { get; set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
      await Task.Delay(2.Seconds(), cancellationToken);

      throw new Hive.Exceptions.ConfigurationException("test configuration exception");
    }
  }
}