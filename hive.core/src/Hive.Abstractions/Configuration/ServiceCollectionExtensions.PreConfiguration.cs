using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MiniValidation;

namespace Hive.Configuration;

/// <summary>
/// All extensions in this file make the configuration accessible before <seealso href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.servicecollectioncontainerbuilderextensions.buildserviceprovider?view=dotnet-plat-ext-7.0">IServiceCollection.BuildServiceProvider()</seealso>
/// is called
/// </summary>
public static partial class ServiceCollectionExtensions
{
  private static readonly string[] DefaultOptionsValidationErrors = new[] { "Options validation failed" };

  /// <summary>
  /// Pre-configures TOptions
  /// </summary>
  /// <typeparam name="TOptions">Type of <see cref="Options"/></typeparam>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <param name="sectionKeyProvider"></param>
  /// <returns><see cref="IOptions{TOptions}"/></returns>
  /// <exception cref="ArgumentNullException">When any of the provided arguments are null</exception>
  public static IOptions<TOptions> PreConfigureOptions<TOptions>(
    this IServiceCollection services,
    IConfiguration configuration,
    Func<string> sectionKeyProvider)
      where TOptions : class, new()
  {
    _ = services ?? throw new ArgumentNullException(nameof(services));
    _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _ = sectionKeyProvider ?? throw new ArgumentNullException(nameof(sectionKeyProvider));

    var options = new TOptions();
    configuration.GetExistingSection(sectionKeyProvider()).Bind(options);

    var optionsInstance = Options.Create(options);
    services.AddSingleton(optionsInstance);

    return optionsInstance;
  }

  /// <summary>
  /// Pre-configures validated TOptions
  /// </summary>
  /// <typeparam name="TOptions">Type of <see cref="Options"/></typeparam>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <param name="sectionKeyProvider"></param>
  /// <returns><see cref="IOptions{TOptions}"/></returns>
  /// <exception cref="ArgumentNullException">When any of the provided arguments are null</exception>
  public static IOptions<TOptions> PreConfigureValidatedOptions<TOptions>(
    this IServiceCollection services,
    IConfiguration configuration,
    Func<string> sectionKeyProvider)
      where TOptions : class, new()
  {
    _ = services ?? throw new ArgumentNullException(nameof(services));
    _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _ = sectionKeyProvider ?? throw new ArgumentNullException(nameof(sectionKeyProvider));

    var options = new TOptions();
    var key = sectionKeyProvider();

    configuration.GetExistingSection(key)
      .Bind(options);

    if (MiniValidator.TryValidate(options, true, out var errors))
    {
      var optionsInstance = Options.Create(options);
      services.AddSingleton(optionsInstance);

      return optionsInstance;
    }

    switch (errors.Count)
    {
      case 1:
        {
          var error = errors.First();
          throw new OptionsValidationException(
            key,
            typeof(TOptions),
            error.Value);
        }

      case > 1:
        throw new OptionsValidationException(
          key,
          typeof(TOptions),
          errors.SelectMany(x => x.Value));
      default:
        throw new InvalidOperationException("Validation failed with no errors returned");
    }
  }

  /// <summary>
  /// Pre-configures validated TOptions
  /// </summary>
  /// <typeparam name="TOptions">Type of <see cref="Options"/></typeparam>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <param name="sectionKeyProvider"></param>
  /// <param name="validate"></param>
  /// <returns><see cref="IOptions{TOptions}"/></returns>
  /// <exception cref="ArgumentNullException">When any of the provided arguments are null</exception>
  public static IOptions<TOptions> PreConfigureValidatedOptions<TOptions>(
    this IServiceCollection services,
    IConfiguration configuration,
    Func<string> sectionKeyProvider,
    Func<TOptions, bool> validate)
      where TOptions : class, new()
  {
    _ = services ?? throw new ArgumentNullException(nameof(services));
    _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _ = sectionKeyProvider ?? throw new ArgumentNullException(nameof(sectionKeyProvider));
    _ = validate ?? throw new ArgumentNullException(nameof(validate));

    var options = new TOptions();
    var key = sectionKeyProvider();

    configuration.GetExistingSection(key)
      .Bind(options);

    if (!validate(options))
    {
      throw new OptionsValidationException(
        key,
        typeof(TOptions),
        DefaultOptionsValidationErrors);
    }

    var optionsInstance = Options.Create(options);
    services.AddSingleton(optionsInstance);

    return optionsInstance;
  }

  /// <summary>
  /// Pre-configures validated TOptions
  /// </summary>
  /// <typeparam name="TOptions">Type of <see cref="Options"/></typeparam>
  /// <typeparam name="TValidator">Type of validator used to validate the options</typeparam>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <param name="sectionKeyProvider"></param>
  /// <returns><see cref="IOptions{TOptions}"/></returns>
  /// <exception cref="ArgumentNullException">When any of the provided arguments are null</exception>
  public static IOptions<TOptions> PreConfigureValidatedOptions<TOptions, TValidator>(
    this IServiceCollection services,
    IConfiguration configuration,
    Func<string> sectionKeyProvider)
      where TOptions : class, new()
      where TValidator : class, IValidator<TOptions>, new()
  {
    _ = services ?? throw new ArgumentNullException(nameof(services));

    var validator = new TValidator();

    return services.PreConfigureValidatedOptions<TOptions, TValidator>(configuration, validator, sectionKeyProvider);
  }

  /// <summary>
  /// Pre-configures validated TOptions
  /// </summary>
  /// <typeparam name="TOptions">Type of <see cref="Options"/></typeparam>
  /// <typeparam name="TValidator">Type of validator used to validate the options</typeparam>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <param name="validator"></param>
  /// <param name="sectionKeyProvider"></param>
  /// <returns><see cref="IOptions{TOptions}"/></returns>
  /// <exception cref="ArgumentNullException">When any of the provided arguments are null</exception>
  public static IOptions<TOptions> PreConfigureValidatedOptions<TOptions, TValidator>(
    this IServiceCollection services,
    IConfiguration configuration,
    TValidator validator,
    Func<string> sectionKeyProvider)
      where TOptions : class, new()
      where TValidator : class, IValidator<TOptions>
  {
    _ = services ?? throw new ArgumentNullException(nameof(services));
    _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _ = sectionKeyProvider ?? throw new ArgumentNullException(nameof(sectionKeyProvider));

    var options = new TOptions();
    var key = sectionKeyProvider();

    configuration.GetExistingSection(key)
      .Bind(options);

    var result = validator.Validate(options);

    if (result.IsValid)
    {
      var optionsInstance = Options.Create(options);
      services.AddSingleton(optionsInstance);

      return optionsInstance;
    }

    switch (result.Errors.Count)
    {
      case 1:
        {
          var error = result.Errors.First();
          throw new OptionsValidationException(
            key,
            typeof(TOptions),
            new[] { error.ErrorMessage });
        }

      case > 1:
        throw new OptionsValidationException(
          key,
          typeof(TOptions),
          result.Errors.Select(x => x.ErrorMessage));
      default:
        throw new InvalidOperationException("Validation failed with no errors returned");
    }
  }

  /// <summary>
  /// Pre-configures validated TOptions from an optional configuration section.
  /// Returns null if the section doesn't exist, allowing the caller to provide defaults.
  /// Uses MiniValidator (DataAnnotations) for validation when section exists.
  /// </summary>
  /// <typeparam name="TOptions">Type of <see cref="Options"/></typeparam>
  /// <param name="services">The service collection</param>
  /// <param name="configuration">The configuration</param>
  /// <param name="sectionKeyProvider">Function that provides the configuration section key</param>
  /// <returns><see cref="IOptions{TOptions}"/> if section exists and is valid, otherwise null</returns>
  /// <exception cref="ArgumentNullException">When any of the provided arguments are null</exception>
  /// <exception cref="OptionsValidationException">When section exists but validation fails</exception>
  public static IOptions<TOptions>? PreConfigureOptionalValidatedOptions<TOptions>(
    this IServiceCollection services,
    IConfiguration configuration,
    Func<string> sectionKeyProvider)
      where TOptions : class, new()
  {
    _ = services ?? throw new ArgumentNullException(nameof(services));
    _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _ = sectionKeyProvider ?? throw new ArgumentNullException(nameof(sectionKeyProvider));

    var options = new TOptions();
    var key = sectionKeyProvider();

    // Check if section exists
    var section = configuration.GetSection(key);
    if (!section.Exists())
    {
      return null;
    }

    // Bind configuration to options
    section.Bind(options);

    // Validate using MiniValidator (DataAnnotations)
    if (MiniValidator.TryValidate(options, true, out var errors))
    {
      var optionsInstance = Options.Create(options);
      services.AddSingleton(optionsInstance);

      return optionsInstance;
    }

    // Validation failed - throw with detailed error messages
    switch (errors.Count)
    {
      case 1:
        {
          var error = errors.First();
          throw new OptionsValidationException(
            key,
            typeof(TOptions),
            error.Value);
        }

      case > 1:
        throw new OptionsValidationException(
          key,
          typeof(TOptions),
          errors.SelectMany(x => x.Value));
      default:
        throw new InvalidOperationException("Validation failed with no errors returned");
    }
  }

  /// <summary>
  /// Pre-configures validated TOptions from an optional configuration section.
  /// Returns null if the section doesn't exist, allowing the caller to provide defaults.
  /// Uses a custom validation delegate when section exists.
  /// </summary>
  /// <typeparam name="TOptions">Type of <see cref="Options"/></typeparam>
  /// <param name="services">The service collection</param>
  /// <param name="configuration">The configuration</param>
  /// <param name="sectionKeyProvider">Function that provides the configuration section key</param>
  /// <param name="validate">Validation function that returns true if valid</param>
  /// <returns><see cref="IOptions{TOptions}"/> if section exists and is valid, otherwise null</returns>
  /// <exception cref="ArgumentNullException">When any of the provided arguments are null</exception>
  /// <exception cref="OptionsValidationException">When section exists but validation fails</exception>
  public static IOptions<TOptions>? PreConfigureOptionalValidatedOptions<TOptions>(
    this IServiceCollection services,
    IConfiguration configuration,
    Func<string> sectionKeyProvider,
    Func<TOptions, bool> validate)
      where TOptions : class, new()
  {
    _ = services ?? throw new ArgumentNullException(nameof(services));
    _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _ = sectionKeyProvider ?? throw new ArgumentNullException(nameof(sectionKeyProvider));
    _ = validate ?? throw new ArgumentNullException(nameof(validate));

    var options = new TOptions();
    var key = sectionKeyProvider();

    // Check if section exists
    var section = configuration.GetSection(key);
    if (!section.Exists())
    {
      return null;
    }

    // Bind configuration to options
    section.Bind(options);

    // Validate using custom delegate
    if (!validate(options))
    {
      throw new OptionsValidationException(
        key,
        typeof(TOptions),
        DefaultOptionsValidationErrors);
    }

    var optionsInstance = Options.Create(options);
    services.AddSingleton(optionsInstance);

    return optionsInstance;
  }

  /// <summary>
  /// Pre-configures validated TOptions from an optional configuration section.
  /// Returns null if the section doesn't exist, allowing the caller to provide defaults.
  /// Uses FluentValidation IValidator for validation when section exists.
  /// </summary>
  /// <typeparam name="TOptions">Type of <see cref="Options"/></typeparam>
  /// <typeparam name="TValidator">Type of FluentValidation validator</typeparam>
  /// <param name="services">The service collection</param>
  /// <param name="configuration">The configuration</param>
  /// <param name="sectionKeyProvider">Function that provides the configuration section key</param>
  /// <returns><see cref="IOptions{TOptions}"/> if section exists and is valid, otherwise null</returns>
  /// <exception cref="ArgumentNullException">When any of the provided arguments are null</exception>
  /// <exception cref="OptionsValidationException">When section exists but validation fails</exception>
  public static IOptions<TOptions>? PreConfigureOptionalValidatedOptions<TOptions, TValidator>(
    this IServiceCollection services,
    IConfiguration configuration,
    Func<string> sectionKeyProvider)
      where TOptions : class, new()
      where TValidator : class, IValidator<TOptions>, new()
  {
    _ = services ?? throw new ArgumentNullException(nameof(services));

    var validator = new TValidator();

    return services.PreConfigureOptionalValidatedOptions<TOptions, TValidator>(
      configuration,
      validator,
      sectionKeyProvider);
  }

  /// <summary>
  /// Pre-configures validated TOptions from an optional configuration section.
  /// Returns null if the section doesn't exist, allowing the caller to provide defaults.
  /// Uses FluentValidation IValidator for validation when section exists.
  /// </summary>
  /// <typeparam name="TOptions">Type of <see cref="Options"/></typeparam>
  /// <typeparam name="TValidator">Type of FluentValidation validator</typeparam>
  /// <param name="services">The service collection</param>
  /// <param name="configuration">The configuration</param>
  /// <param name="validator">FluentValidation validator instance</param>
  /// <param name="sectionKeyProvider">Function that provides the configuration section key</param>
  /// <returns><see cref="IOptions{TOptions}"/> if section exists and is valid, otherwise null</returns>
  /// <exception cref="ArgumentNullException">When any of the provided arguments are null</exception>
  /// <exception cref="OptionsValidationException">When section exists but validation fails</exception>
  public static IOptions<TOptions>? PreConfigureOptionalValidatedOptions<TOptions, TValidator>(
    this IServiceCollection services,
    IConfiguration configuration,
    TValidator validator,
    Func<string> sectionKeyProvider)
      where TOptions : class, new()
      where TValidator : class, IValidator<TOptions>
  {
    _ = services ?? throw new ArgumentNullException(nameof(services));
    _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _ = sectionKeyProvider ?? throw new ArgumentNullException(nameof(sectionKeyProvider));

    var options = new TOptions();
    var key = sectionKeyProvider();

    // Check if section exists
    var section = configuration.GetSection(key);
    if (!section.Exists())
    {
      return null;
    }

    // Bind configuration to options
    section.Bind(options);

    // Validate using FluentValidation
    var result = validator.Validate(options);

    if (result.IsValid)
    {
      var optionsInstance = Options.Create(options);
      services.AddSingleton(optionsInstance);

      return optionsInstance;
    }

    // Validation failed - throw with detailed error messages
    switch (result.Errors.Count)
    {
      case 1:
        {
          var error = result.Errors.First();
          throw new OptionsValidationException(
            key,
            typeof(TOptions),
            new[] { error.ErrorMessage });
        }

      case > 1:
        throw new OptionsValidationException(
          key,
          typeof(TOptions),
          result.Errors.Select(x => x.ErrorMessage));
      default:
        throw new InvalidOperationException("Validation failed with no errors returned");
    }
  }
}