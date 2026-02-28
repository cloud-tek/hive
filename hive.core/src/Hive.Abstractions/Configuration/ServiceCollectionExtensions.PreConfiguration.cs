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
  /// Adds a singleton IOptions instance to the service collection if not already registered.
  /// Returns the registered instance (either newly added or existing).
  /// </summary>
  private static IOptions<TOptions> AddOrGetSingletonOptions<TOptions>(
    this IServiceCollection services,
    TOptions options)
    where TOptions : class
  {
    var optionsInstance = Options.Create(options);

    // Check if IOptions<TOptions> is already registered
    var existingDescriptor = services.FirstOrDefault(sd =>
      sd.ServiceType == typeof(IOptions<TOptions>) &&
      sd.Lifetime == ServiceLifetime.Singleton);

    if (existingDescriptor != null)
    {
      // If registered with an instance, return it
      if (existingDescriptor.ImplementationInstance != null)
      {
        return (IOptions<TOptions>)existingDescriptor.ImplementationInstance;
      }

      // If registered with a factory or type, build temporary provider to resolve
      // This ensures we return the actual registered instance
      var serviceProvider = services.BuildServiceProvider();
      return serviceProvider.GetRequiredService<IOptions<TOptions>>();
    }

    // Not registered yet, add it
    services.AddSingleton(optionsInstance);
    return optionsInstance;
  }

  /// <summary>
  /// Throws an <see cref="OptionsValidationException"/> with the collected validation errors.
  /// </summary>
  private static void ThrowValidationErrors(string key, Type optionsType, IEnumerable<string> errors)
  {
    var errorList = errors.ToList();
    if (errorList.Count == 0)
    {
      throw new InvalidOperationException("Validation failed with no errors returned");
    }

    throw new OptionsValidationException(key, optionsType, errorList);
  }

  /// <summary>
  /// Core implementation for pre-configuring validated options from a required configuration section.
  /// </summary>
  private static IOptions<TOptions> PreConfigureValidatedOptionsCore<TOptions>(
    IServiceCollection services,
    IConfiguration configuration,
    string key,
    Func<TOptions, (bool IsValid, IEnumerable<string> Errors)> validate)
    where TOptions : class, new()
  {
    var options = new TOptions();
    configuration.GetExistingSection(key).Bind(options);

    var (isValid, errors) = validate(options);
    if (isValid)
    {
      return services.AddOrGetSingletonOptions(options);
    }

    ThrowValidationErrors(key, typeof(TOptions), errors);
    return null!; // unreachable
  }

  /// <summary>
  /// Core implementation for pre-configuring validated options from an optional configuration section.
  /// Returns null if the section doesn't exist.
  /// </summary>
  private static IOptions<TOptions>? PreConfigureOptionalValidatedOptionsCore<TOptions>(
    IServiceCollection services,
    IConfiguration configuration,
    string key,
    Func<TOptions, (bool IsValid, IEnumerable<string> Errors)> validate)
    where TOptions : class, new()
  {
    var section = configuration.GetSection(key);
    if (!section.Exists())
    {
      return null;
    }

    var options = new TOptions();
    section.Bind(options);

    var (isValid, errors) = validate(options);
    if (isValid)
    {
      return services.AddOrGetSingletonOptions(options);
    }

    ThrowValidationErrors(key, typeof(TOptions), errors);
    return null; // unreachable
  }

  /// <summary>
  /// Creates a validation strategy using MiniValidator (DataAnnotations).
  /// </summary>
  private static (bool IsValid, IEnumerable<string> Errors) ValidateWithDataAnnotations<TOptions>(TOptions options)
  {
    if (MiniValidator.TryValidate(options, true, out var errors))
    {
      return (true, Enumerable.Empty<string>());
    }

    return (false, errors.SelectMany(x => x.Value));
  }

  /// <summary>
  /// Creates a validation strategy using FluentValidation.
  /// </summary>
  private static (bool IsValid, IEnumerable<string> Errors) ValidateWithFluentValidation<TOptions, TValidator>(
    TOptions options,
    TValidator validator)
    where TValidator : IValidator<TOptions>
  {
    var result = validator.Validate(options);
    if (result.IsValid)
    {
      return (true, Enumerable.Empty<string>());
    }

    return (false, result.Errors.Select(x => x.ErrorMessage));
  }

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

    return services.AddOrGetSingletonOptions(options);
  }

  /// <summary>
  /// Pre-configures validated TOptions using DataAnnotations (MiniValidator)
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

    return PreConfigureValidatedOptionsCore<TOptions>(
      services, configuration, sectionKeyProvider(),
      ValidateWithDataAnnotations);
  }

  /// <summary>
  /// Pre-configures validated TOptions using a custom validation delegate
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

    return PreConfigureValidatedOptionsCore<TOptions>(
      services, configuration, sectionKeyProvider(),
      opts => validate(opts)
        ? (true, Enumerable.Empty<string>())
        : (false, DefaultOptionsValidationErrors));
  }

  /// <summary>
  /// Pre-configures validated TOptions using FluentValidation (creates new validator instance)
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

    return services.PreConfigureValidatedOptions<TOptions, TValidator>(
      configuration, new TValidator(), sectionKeyProvider);
  }

  /// <summary>
  /// Pre-configures validated TOptions using a FluentValidation validator instance
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

    return PreConfigureValidatedOptionsCore<TOptions>(
      services, configuration, sectionKeyProvider(),
      opts => ValidateWithFluentValidation(opts, validator));
  }

  /// <summary>
  /// Pre-configures validated TOptions from an optional configuration section using DataAnnotations.
  /// Returns null if the section doesn't exist, allowing the caller to provide defaults.
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

    return PreConfigureOptionalValidatedOptionsCore<TOptions>(
      services, configuration, sectionKeyProvider(),
      ValidateWithDataAnnotations);
  }

  /// <summary>
  /// Pre-configures validated TOptions from an optional configuration section using a delegate.
  /// Returns null if the section doesn't exist, allowing the caller to provide defaults.
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

    return PreConfigureOptionalValidatedOptionsCore<TOptions>(
      services, configuration, sectionKeyProvider(),
      opts => validate(opts)
        ? (true, Enumerable.Empty<string>())
        : (false, DefaultOptionsValidationErrors));
  }

  /// <summary>
  /// Pre-configures validated TOptions from an optional configuration section using FluentValidation.
  /// Returns null if the section doesn't exist. Creates a new validator instance.
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

    return services.PreConfigureOptionalValidatedOptions<TOptions, TValidator>(
      configuration, new TValidator(), sectionKeyProvider);
  }

  /// <summary>
  /// Pre-configures validated TOptions from an optional configuration section using a FluentValidation validator instance.
  /// Returns null if the section doesn't exist.
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

    return PreConfigureOptionalValidatedOptionsCore<TOptions>(
      services, configuration, sectionKeyProvider(),
      opts => ValidateWithFluentValidation(opts, validator));
  }
}