using FluentAssertions;

namespace Hive.Testing;

/// <summary>
/// A set of extensions for testing microservices
/// </summary>
public static class MicroServiceTestExtensions
{
  /// <summary>
  /// Waits for the service to start and checks that it is ready and started
  /// </summary>
  /// <param name="service"></param>
  /// <param name="timeout"></param>
  public static void ShouldStart(this IMicroService service, TimeSpan timeout)
  {
    service.Lifetime.ServiceStarted.WaitHandle.WaitOne(timeout);

    service.IsReady.Should().BeTrue();
    service.IsStarted.Should().BeTrue();
  }

  /// <summary>
  /// Waits for the service to fail to start and checks that it is not ready and not started
  /// </summary>
  /// <param name="service"></param>
  /// <param name="timeout"></param>
  public static void ShouldFailToStart(this IMicroService service, TimeSpan timeout)
  {
    service.Lifetime.StartupFailed.WaitHandle.WaitOne(timeout);
    service.Lifetime.StartupFailed.IsCancellationRequested.Should().BeTrue();

    service.IsReady.Should().BeFalse();
    service.IsStarted.Should().BeFalse();
  }
}