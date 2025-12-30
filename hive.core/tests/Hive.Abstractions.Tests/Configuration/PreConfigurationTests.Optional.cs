using FluentAssertions;
using Hive.Configuration;
using Hive.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hive.Abstractions.Tests.Configuration;

public partial class PreConfigurationTests
{
  #region PreConfigureOptionalValidatedOptions - DataAnnotations

  [Fact]
  [UnitTest]
  public void GivenSectionDoesNotExist_WhenPreConfigureOptionalValidatedOptions_ThenReturnsNull()
  {
    // Arrange
    var cfg = new ConfigurationBuilder().Build();

    // Act
    var options = new ServiceCollection()
      .PreConfigureOptionalValidatedOptions<SimpleOptions>(cfg, () => "NonExistent");

    // Assert
    options.Should().BeNull();
  }

  [SmartTheory(Execute.Always, On.All)]
  [InlineData("simple-options-01.json", "Test")]
  [UnitTest]
  public void GivenSectionExists_WhenPreConfigureOptionalValidatedOptions_ThenReturnsValidatedOptions(
    string config, string expectedName)
  {
    // Arrange
    var cfg = GetConfigurationRoot(config);

    // Act
    var options = new ServiceCollection()
      .PreConfigureOptionalValidatedOptions<SimpleOptions>(cfg, () => SimpleOptions.SectionKey);

    // Assert
    options.Should().NotBeNull();
    options!.Value.Should().NotBeNull();
    options.Value.Name.Should().Be(expectedName);
  }

  #endregion

  #region PreConfigureOptionalValidatedOptions - Delegate

  [Fact]
  [UnitTest]
  public void GivenSectionDoesNotExist_WhenPreConfigureOptionalValidatedOptionsWithDelegate_ThenReturnsNull()
  {
    // Arrange
    var cfg = new ConfigurationBuilder().Build();

    // Act
    var options = new ServiceCollection()
      .PreConfigureOptionalValidatedOptions<SimpleOptions>(
        cfg,
        () => "NonExistent",
        opts => opts.Name != null);

    // Assert
    options.Should().BeNull();
  }

  [SmartTheory(Execute.Always, On.All)]
  [InlineData("simple-options-01.json", "Test")]
  [UnitTest]
  public void GivenSectionExists_WhenPreConfigureOptionalValidatedOptionsWithDelegate_ThenReturnsValidatedOptions(
    string config, string expectedName)
  {
    // Arrange
    var cfg = GetConfigurationRoot(config);

    // Act
    var options = new ServiceCollection()
      .PreConfigureOptionalValidatedOptions<SimpleOptions>(
        cfg,
        () => SimpleOptions.SectionKey,
        opts => opts.Name == expectedName);

    // Assert
    options.Should().NotBeNull();
    options!.Value.Should().NotBeNull();
    options.Value.Name.Should().Be(expectedName);
  }

  [Fact]
  [UnitTest]
  public void GivenInvalidSection_WhenPreConfigureOptionalValidatedOptionsWithDelegate_ThenThrowsOptionsValidationException()
  {
    // Arrange
    var cfg = GetConfigurationRoot("simple-options-01.json");

    // Act
    var action = () => new ServiceCollection()
      .PreConfigureOptionalValidatedOptions<SimpleOptions>(
        cfg,
        () => SimpleOptions.SectionKey,
        opts => opts.Name == "InvalidValue"); // Will fail validation

    // Assert
    action.Should().Throw<OptionsValidationException>()
      .WithMessage("*validation failed*");
  }

  #endregion

  #region PreConfigureOptionalValidatedOptions - FluentValidation

  [Fact]
  [UnitTest]
  public void GivenSectionDoesNotExist_WhenPreConfigureOptionalValidatedOptionsWithFluentValidation_ThenReturnsNull()
  {
    // Arrange
    var cfg = new ConfigurationBuilder().Build();

    // Act
    var options = new ServiceCollection()
      .PreConfigureOptionalValidatedOptions<SimpleOptions, SimpleOptionsFluentValidator>(
        cfg,
        () => "NonExistent");

    // Assert
    options.Should().BeNull();
  }

  [SmartTheory(Execute.Always, On.All)]
  [InlineData("simple-options-01.json", "Test")]
  [UnitTest]
  public void GivenSectionExists_WhenPreConfigureOptionalValidatedOptionsWithFluentValidation_ThenReturnsValidatedOptions(
    string config, string expectedValue)
  {
    // Arrange
    var cfg = GetConfigurationRoot(config);

    // Act
    var options = new ServiceCollection()
      .PreConfigureOptionalValidatedOptions<SimpleOptions, SimpleOptionsFluentValidator>(
        cfg,
        () => SimpleOptions.SectionKey);

    // Assert
    options.Should().NotBeNull();
    options!.Value.Should().NotBeNull();
    options.Value.Name.Should().Be(expectedValue);
  }

  [SmartTheory(Execute.Always, On.All)]
  [InlineData("simple-options-02.json")]
  [InlineData("simple-options-03.json")]
  [UnitTest]
  public void GivenInvalidSection_WhenPreConfigureOptionalValidatedOptionsWithFluentValidation_ThenThrowsOptionsValidationException(
    string config)
  {
    // Arrange
    var cfg = GetConfigurationRoot(config);

    // Act
    var action = () => new ServiceCollection()
      .PreConfigureOptionalValidatedOptions<SimpleOptions, SimpleOptionsFluentValidator>(
        cfg,
        () => SimpleOptions.SectionKey);

    // Assert
    action.Should().Throw<OptionsValidationException>()
      .WithMessage("*Name*");
  }

  [Fact]
  [UnitTest]
  public void GivenSectionDoesNotExist_WhenPreConfigureOptionalValidatedOptionsWithValidatorInstance_ThenReturnsNull()
  {
    // Arrange
    var cfg = new ConfigurationBuilder().Build();
    var validator = new SimpleOptionsFluentValidator();

    // Act
    var options = new ServiceCollection()
      .PreConfigureOptionalValidatedOptions<SimpleOptions, SimpleOptionsFluentValidator>(
        cfg,
        validator,
        () => "NonExistent");

    // Assert
    options.Should().BeNull();
  }

  [SmartTheory(Execute.Always, On.All)]
  [InlineData("simple-options-01.json", "Test")]
  [UnitTest]
  public void GivenSectionExists_WhenPreConfigureOptionalValidatedOptionsWithValidatorInstance_ThenReturnsValidatedOptions(
    string config, string expectedValue)
  {
    // Arrange
    var cfg = GetConfigurationRoot(config);
    var validator = new SimpleOptionsFluentValidator();

    // Act
    var options = new ServiceCollection()
      .PreConfigureOptionalValidatedOptions<SimpleOptions, SimpleOptionsFluentValidator>(
        cfg,
        validator,
        () => SimpleOptions.SectionKey);

    // Assert
    options.Should().NotBeNull();
    options!.Value.Should().NotBeNull();
    options.Value.Name.Should().Be(expectedValue);
  }

  #endregion
}