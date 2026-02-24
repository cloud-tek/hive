using FluentAssertions;
using Hive.Messaging.Middleware;
using Hive.MicroServices;
using Hive.Testing;
using Xunit;

namespace Hive.Messaging.Tests;

public class ReadinessGateMiddlewareTests
{
  [Fact]
  [UnitTest]
  public void GivenServiceIsReady_WhenBeforeCalled_ThenNoExceptionThrown()
  {
    var microService = new MicroService("test-service");
    microService.IsStarted = true;
    microService.IsReady = true;

    var act = () => ReadinessGateMiddleware.Before(microService);

    act.Should().NotThrow();
  }

  [Fact]
  [UnitTest]
  public void GivenServiceIsNotReady_WhenBeforeCalled_ThenThrowsServiceNotReadyException()
  {
    var microService = new MicroService("test-service");

    var act = () => ReadinessGateMiddleware.Before(microService);

    act.Should().Throw<ServiceNotReadyException>()
      .WithMessage("*test-service*not ready*");
  }

  [Fact]
  [UnitTest]
  public void GivenServiceBecameNotReady_WhenBeforeCalled_ThenThrowsServiceNotReadyException()
  {
    var microService = new MicroService("test-service");
    microService.IsStarted = true;
    microService.IsReady = true;

    microService.IsReady = false;

    var act = () => ReadinessGateMiddleware.Before(microService);

    act.Should().Throw<ServiceNotReadyException>();
  }

  [Fact]
  [UnitTest]
  public void GivenServiceNotStarted_WhenBeforeCalled_ThenThrowsServiceNotReadyException()
  {
    var microService = new MicroService("test-service");
    microService.IsReady = true;

    var act = () => ReadinessGateMiddleware.Before(microService);

    act.Should().Throw<ServiceNotReadyException>();
  }
}
