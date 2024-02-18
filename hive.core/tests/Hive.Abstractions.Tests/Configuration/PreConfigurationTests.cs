using FluentAssertions;
using Hive.Configuration;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hive.Tests.Configuration;

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
}