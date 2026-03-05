using CloudTek.Testing;
using FluentAssertions;
using Hive.Messaging.Configuration;
using Hive.Messaging.RabbitMq.Configuration;
using Hive.Messaging.RabbitMq.Transport;
using Microsoft.Extensions.Configuration;
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

  [Fact]
  [UnitTest]
  public void GivenProviderWithNamedBrokerOptions_WhenValidated_ThenFluentOptionsAreUsed()
  {
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Hive:Messaging:Transport"] = "RabbitMQ",
        ["Hive:Messaging:RabbitMq:ConnectionUri"] = "amqp://localhost:5672"
      })
      .Build();

    var provider = new RabbitMqTransportProvider();
    provider.AddNamedBrokerOptions("secondary", new RabbitMqOptions
    {
      ConnectionUri = "amqp://secondary:5672",
      AutoProvision = true
    });

    var options = new MessagingOptions
    {
      Transport = MessagingTransport.RabbitMQ,
      NamedBrokers = new Dictionary<string, NamedBrokerOptions>
      {
        ["secondary"] = new NamedBrokerOptions()
      }
    };

    var errors = provider.Validate(options, config).ToList();
    errors.Should().BeEmpty();
  }
}