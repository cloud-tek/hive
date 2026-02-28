using CloudTek.Testing;
using FluentAssertions;
using Hive.Messaging.Configuration;
using Hive.Messaging.RabbitMq;
using Hive.Messaging.RabbitMq.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Hive.Messaging.RabbitMq.Tests;

public class ConfigurationTests
{
  [Fact]
  [UnitTest]
  public void GivenMinimalConfig_WhenTransportIsRabbitMqWithUri_ThenValidationSucceeds()
  {
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Hive:Messaging:Transport"] = "RabbitMQ",
        ["Hive:Messaging:RabbitMq:ConnectionUri"] = "amqp://localhost:5672"
      })
      .Build();

    var options = new MessagingOptions { Transport = MessagingTransport.RabbitMQ };
    var provider = new RabbitMqTransportProvider();

    var errors = provider.Validate(options, config).ToList();

    errors.Should().BeEmpty();
  }

  [Fact]
  [UnitTest]
  public void GivenRabbitMqTransport_WhenConnectionUriIsMissing_ThenValidationFails()
  {
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Hive:Messaging:Transport"] = "RabbitMQ"
      })
      .Build();

    var options = new MessagingOptions { Transport = MessagingTransport.RabbitMQ };
    var provider = new RabbitMqTransportProvider();

    var errors = provider.Validate(options, config).ToList();

    errors.Should().NotBeEmpty();
    errors.Should().Contain(e => e.Contains("ConnectionUri is required"));
  }

  [Fact]
  [UnitTest]
  public void GivenRabbitMqTransport_WhenConnectionUriIsInvalid_ThenValidationFails()
  {
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Hive:Messaging:Transport"] = "RabbitMQ",
        ["Hive:Messaging:RabbitMq:ConnectionUri"] = "not-a-valid-uri"
      })
      .Build();

    var options = new MessagingOptions { Transport = MessagingTransport.RabbitMQ };
    var provider = new RabbitMqTransportProvider();

    var errors = provider.Validate(options, config).ToList();

    errors.Should().NotBeEmpty();
    errors.Should().Contain(e => e.Contains("valid absolute URI"));
  }

  [Fact]
  [UnitTest]
  public void GivenInMemoryTransport_WhenNoConnectionUri_ThenValidationSucceeds()
  {
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>())
      .Build();

    var options = new MessagingOptions { Transport = MessagingTransport.InMemory };
    var provider = new RabbitMqTransportProvider();

    var errors = provider.Validate(options, config).ToList();

    errors.Should().BeEmpty();
  }

  [Fact]
  [UnitTest]
  public void GivenNamedBroker_WhenConnectionUriIsMissing_ThenValidationFails()
  {
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Hive:Messaging:Transport"] = "RabbitMQ",
        ["Hive:Messaging:RabbitMq:ConnectionUri"] = "amqp://localhost:5672"
      })
      .Build();

    var options = new MessagingOptions
    {
      Transport = MessagingTransport.RabbitMQ,
      NamedBrokers = new Dictionary<string, NamedBrokerOptions>
      {
        ["secondary"] = new NamedBrokerOptions()
      }
    };
    var provider = new RabbitMqTransportProvider();

    var errors = provider.Validate(options, config).ToList();

    errors.Should().NotBeEmpty();
  }

  [Fact]
  [UnitTest]
  public void GivenNamedBroker_WhenConnectionUriIsValid_ThenValidationSucceeds()
  {
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Hive:Messaging:Transport"] = "RabbitMQ",
        ["Hive:Messaging:RabbitMq:ConnectionUri"] = "amqp://localhost:5672",
        ["Hive:Messaging:NamedBrokers:secondary:RabbitMq:ConnectionUri"] = "amqp://secondary:5672"
      })
      .Build();

    var options = new MessagingOptions
    {
      Transport = MessagingTransport.RabbitMQ,
      NamedBrokers = new Dictionary<string, NamedBrokerOptions>
      {
        ["secondary"] = new NamedBrokerOptions()
      }
    };
    var provider = new RabbitMqTransportProvider();

    var errors = provider.Validate(options, config).ToList();

    errors.Should().BeEmpty();
  }

  [Fact]
  [UnitTest]
  public void GivenJsonConfiguration_WhenBound_ThenOptionsArePopulated()
  {
    var config = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Hive:Messaging:Transport"] = "RabbitMQ",
        ["Hive:Messaging:RabbitMq:ConnectionUri"] = "amqp://localhost:5672",
        ["Hive:Messaging:RabbitMq:AutoProvision"] = "true",
        ["Hive:Messaging:Serialization"] = "SystemTextJson",
        ["Hive:Messaging:Handling:PrefetchCount"] = "10",
        ["Hive:Messaging:Handling:ListenerCount"] = "2"
      })
      .Build();

    var options = new MessagingOptions();
    config.GetSection(MessagingOptions.SectionKey).Bind(options);

    options.Transport.Should().Be(MessagingTransport.RabbitMQ);
    options.Serialization.Should().Be(MessagingSerialization.SystemTextJson);
    options.Handling.PrefetchCount.Should().Be(10);
    options.Handling.ListenerCount.Should().Be(2);

    // RabbitMQ options are bound by the provider from IConfiguration
    var rmqSection = config.GetSection($"{MessagingOptions.SectionKey}:RabbitMq");
    var rmqOptions = new RabbitMqOptions();
    rmqSection.Bind(rmqOptions);

    rmqOptions.ConnectionUri.Should().Be("amqp://localhost:5672");
    rmqOptions.AutoProvision.Should().BeTrue();
  }
}