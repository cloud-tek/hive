﻿using FluentAssertions.Extensions;
using Hive.MicroServices.Lifecycle;
using System.Threading;
using System.Threading.Tasks;

namespace Hive.MicroServices.Tests;

internal static class TestData
{
  // ReSharper disable once ClassNeverInstantiated.Global
  internal class Sec2DelayStartupService : IHostedStartupService
    {
        public bool Completed { get; set; } = false;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(2.Seconds());

            Completed = true;
        }
    }

  // ReSharper disable once ClassNeverInstantiated.Global
  internal class FailingSec2DelayStartupService : IHostedStartupService
    {
        public bool Completed { get; set; } = false;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(2.Seconds());

            throw new Hive.Exceptions.ConfigurationException("test configuration exception");
        }
    }
}
