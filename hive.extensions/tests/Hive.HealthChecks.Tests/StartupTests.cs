using FluentAssertions;
using Hive.HealthChecks;
using Hive.MicroServices;
using CloudTek.Testing;
using Xunit;

namespace Hive.HealthChecks.Tests;

public class StartupTests
{
  [Fact]
  [UnitTest]
  public void GivenMicroService_WhenWithHealthChecksCalled_ThenExtensionIsRegistered()
  {
    var service = new MicroService("test-service");

    service.WithHealthChecks();

    service.Extensions.Should().ContainSingle(e => e is HealthChecksExtension);
  }

  [Fact]
  [UnitTest]
  public void GivenMicroService_WhenWithHealthChecksCalledTwice_ThenThrows()
  {
    var service = new MicroService("test-service");
    service.WithHealthChecks();

    var act = () => service.WithHealthChecks();

    act.Should().Throw<InvalidOperationException>()
      .WithMessage("*already been called*");
  }

  [Fact]
  [UnitTest]
  public void GivenMicroService_WhenWithHealthChecksCalledWithoutCallback_ThenNoChecksRegistered()
  {
    var service = new MicroService("test-service");

    service.WithHealthChecks();

    var extension = service.Extensions.OfType<HealthChecksExtension>().Single();
    extension.Should().NotBeNull();
  }
}