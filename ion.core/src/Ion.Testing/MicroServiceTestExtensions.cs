using FluentAssertions;
using System;

namespace Ion.Testing;

public static class MicroServiceTestExtensions
{
    public static void ShouldStart(this IMicroService service, TimeSpan timeout)
    {
        service.Lifetime.ServiceStarted.WaitHandle.WaitOne(timeout);

        service.IsReady.Should().BeTrue();
        service.IsStarted.Should().BeTrue();
    }

    public static void ShouldFailToStart(this IMicroService service, TimeSpan timeout)
    {
        service.Lifetime.StartupFailed.WaitHandle.WaitOne(timeout);
        service.Lifetime.StartupFailed.IsCancellationRequested.Should().BeTrue();

        service.IsReady.Should().BeFalse();
        service.IsStarted.Should().BeFalse();
    }
}