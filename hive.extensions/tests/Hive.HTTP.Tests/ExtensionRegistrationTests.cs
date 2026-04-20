using CloudTek.Testing;
using FluentAssertions;
using Hive.MicroServices;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hive.HTTP.Tests;

public class ExtensionRegistrationTests
{
  private const string ServiceName = "http-extension-tests";

  [Fact]
  [UnitTest]
  public void GivenWithHttpClient_WhenCalled_ThenExtensionIsRegistered()
  {
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());

    service.WithHttpClient<IProductApi>(client => client
      .Internal()
      .WithBaseAddress("https://product-service"));

    service.Extensions.Should().ContainSingle(e => e is Extension);
  }

  [Fact]
  [UnitTest]
  public void GivenWithHttpClient_WhenCalledMultipleTimes_ThenSingleExtensionIsRegistered()
  {
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());

    service
      .WithHttpClient<IProductApi>(client => client
        .Internal()
        .WithBaseAddress("https://product-service"))
      .WithHttpClient<IInventoryApi>(client => client
        .Internal()
        .WithBaseAddress("https://inventory-service"));

    service.Extensions.Should().ContainSingle(e => e is Extension);
  }

  [Fact]
  [UnitTest]
  public void GivenWithHttpClient_WhenParameterless_ThenExtensionIsRegistered()
  {
    var service = new MicroService(ServiceName, new NullLogger<IMicroService>());

    service.WithHttpClient<IProductApi>();

    service.Extensions.Should().ContainSingle(e => e is Extension);
  }
}