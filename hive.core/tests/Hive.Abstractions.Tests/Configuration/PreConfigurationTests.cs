using FluentAssertions;
using Hive.Configuration;
using CloudTek.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hive.Abstractions.Tests.Configuration;

public partial class PreConfigurationTests : ConfigurationFixture
{
  [SmartTheory(Execute.Always, On.All)]
  [InlineData("simple-options-01.json", "Test")]
  [UnitTest]
  public void
    GivenSectionExists_WhenPreConfigureOptions_ThenOptionsAreAvailableImmediatelyAndPropertiesAreBound(
      string config, string expectedName)
  {
    // Arrange
    var cfg = GetConfigurationRoot(config);

    // Act
    var options = new ServiceCollection()
      .PreConfigureOptions<SimpleOptions>(cfg, () => SimpleOptions.SectionKey);

    // Assert
    options.Value.Should().NotBeNull();
    options.Value.Name.Should().NotBeNullOrEmpty();
    options.Value.Name.Should().Be(expectedName);
  }

  [SmartTheory(Execute.Always, On.All)]
  [InlineData("simple-options-01.json", "Test")]
  [UnitTest]
  public void
    GivenOptionsAlreadyRegisteredWithFactory_WhenPreConfigureOptions_ThenReturnsExistingRegistrationWithoutNullReference(
      string config, string expectedName)
  {
    // Arrange
    var cfg = GetConfigurationRoot(config);
    var services = new ServiceCollection();

    // Pre-register IOptions<SimpleOptions> using a factory (not an instance)
    services.AddSingleton<IOptions<SimpleOptions>>(sp =>
      Options.Create(new SimpleOptions { Name = expectedName, Address = "Factory Street" }));

    // Act - This should NOT throw NullReferenceException
    var options = services.PreConfigureOptions<SimpleOptions>(cfg, () => SimpleOptions.SectionKey);

    // Assert
    options.Value.Should().NotBeNull();
    options.Value.Name.Should().Be(expectedName);
    options.Value.Address.Should().Be("Factory Street");
  }

  [SmartTheory(Execute.Always, On.All)]
  [InlineData("simple-options-01.json", "Test")]
  [UnitTest]
  public void
    GivenOptionsAlreadyRegisteredWithImplementationType_WhenPreConfigureOptions_ThenReturnsExistingRegistrationWithoutNullReference(
      string config, string expectedName)
  {
    // Arrange
    var cfg = GetConfigurationRoot(config);
    var services = new ServiceCollection();

    // Pre-register using implementation type
    services.AddSingleton<IOptions<SimpleOptions>, OptionsManager<SimpleOptions>>();
    services.Configure<SimpleOptions>(opts =>
    {
      opts.Name = expectedName;
      opts.Address = "Type Street";
    });

    // Act - This should NOT throw NullReferenceException
    var options = services.PreConfigureOptions<SimpleOptions>(cfg, () => SimpleOptions.SectionKey);

    // Assert
    options.Value.Should().NotBeNull();
    options.Value.Name.Should().Be(expectedName);
    options.Value.Address.Should().Be("Type Street");
  }
}