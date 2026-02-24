using FluentAssertions;
using Hive.Messaging.RabbitMq.Configuration;
using Hive.Messaging.RabbitMq.Transport;
using Hive.Testing;
using Xunit;

namespace Hive.Messaging.RabbitMq.Tests;

public class BuilderTests
{
  [Fact]
  [UnitTest]
  public void GivenRabbitMqTransportBuilder_WhenConfigured_ThenOptionsAreSet()
  {
    var builder = new RabbitMqTransportBuilder();

    builder
      .ConnectionUri("amqp://localhost:5672")
      .AutoProvision();

    builder.Options.ConnectionUri.Should().Be("amqp://localhost:5672");
    builder.Options.AutoProvision.Should().BeTrue();
  }

  [Fact]
  [UnitTest]
  public void GivenRabbitMqOptions_WhenDefaultsUsed_ThenAutoProvisionIsFalse()
  {
    var options = new RabbitMqOptions();

    options.ConnectionUri.Should().BeNull();
    options.AutoProvision.Should().BeFalse();
  }
}
