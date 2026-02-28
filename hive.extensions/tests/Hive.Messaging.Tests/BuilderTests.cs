using CloudTek.Testing;
using FluentAssertions;
using Hive.Messaging.Configuration;
using Xunit;

namespace Hive.Messaging.Tests;

public class BuilderTests
{
  [Fact]
  [UnitTest]
  public void GivenMessagingOptions_WhenDefaultsUsed_ThenTransportIsInMemory()
  {
    var options = new MessagingOptions();

    options.Transport.Should().Be(MessagingTransport.InMemory);
    options.Serialization.Should().Be(MessagingSerialization.SystemTextJson);
    options.Handling.Should().NotBeNull();
    options.NamedBrokers.Should().BeEmpty();
  }

  [Fact]
  [UnitTest]
  public void GivenHandlingOptions_WhenDefaultsUsed_ThenNullable()
  {
    var options = new HandlingOptions();

    options.PrefetchCount.Should().BeNull();
    options.ListenerCount.Should().BeNull();
  }
}