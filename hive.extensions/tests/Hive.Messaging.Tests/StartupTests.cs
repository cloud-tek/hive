using FluentAssertions;
using Hive.MicroServices;
using CloudTek.Testing;
using Xunit;

namespace Hive.Messaging.Tests;

public class StartupTests
{
  [Fact]
  [UnitTest]
  public void GivenMicroService_WhenWithMessagingCalled_ThenExtensionIsRegistered()
  {
    var service = new MicroService("test-service");

    service.WithMessaging(builder =>
      builder.UseInMemoryTransport());

    service.Extensions.Should().ContainSingle(
      e => e.GetType().Name == "MessagingExtension");
  }

  [Fact]
  [UnitTest]
  public void GivenMicroService_WhenWithMessagingCalledTwice_ThenThrowsInvalidOperationException()
  {
    var service = new MicroService("test-service");

    service.WithMessaging(builder =>
      builder.UseInMemoryTransport());

    var act = () => service.WithMessaging(builder =>
      builder.UseInMemoryTransport());

    act.Should().Throw<InvalidOperationException>()
      .WithMessage("*already been called*");
  }

  [Fact]
  [UnitTest]
  public void GivenMicroServiceCore_WhenWithMessagingSendOnlyCalled_ThenExtensionIsRegistered()
  {
    var service = new MicroService("test-service");

    ((IMicroServiceCore)service).WithMessaging(builder =>
      builder.UseInMemoryTransport());

    service.Extensions.Should().ContainSingle(
      e => e.GetType().Name == "MessagingSendExtension");
  }

  [Fact]
  [UnitTest]
  public void GivenNullService_WhenWithMessagingCalled_ThenThrowsArgumentNullException()
  {
    IMicroService service = null!;

    var act = () => service.WithMessaging(builder =>
      builder.UseInMemoryTransport());

    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  [UnitTest]
  public void GivenNullConfigure_WhenWithMessagingCalled_ThenThrowsArgumentNullException()
  {
    var service = new MicroService("test-service");

    var act = () => service.WithMessaging((Action<HiveMessagingBuilder>)null!);

    act.Should().Throw<ArgumentNullException>();
  }
}