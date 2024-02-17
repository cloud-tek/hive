using FluentValidation;
using Hive.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hive.Configuration;

/// <summary>
/// All extensions in this file make the configuration accessible after <seealso href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.servicecollectioncontainerbuilderextensions.buildserviceprovider?view=dotnet-plat-ext-7.0">IServiceCollection.BuildServiceProvider()</seealso>
/// </summary>
public static partial class ServiceCollectionExtensions
{
  private static readonly Action<BinderOptions> DefaultBinderOptions = (o) =>
  {
    o.BindNonPublicProperties = true;
    o.ErrorOnUnknownConfiguration = true;
  };

  /// <summary>
  /// Configures TOptions
  /// </summary>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <param name="sectionKeyProvider"></param>
  /// <typeparam name="TOptions">Type of <see cref="Options"/></typeparam>
  /// <returns><see cref="IServiceCollection"/>></returns>
  public static IServiceCollection ConfigureOptions<TOptions>(
    this IServiceCollection services,
    IConfiguration configuration,
    Func<string> sectionKeyProvider)
      where TOptions : class
  {
    services.Configure<TOptions>(configuration.GetExistingSection(sectionKeyProvider()));
    return services;
  }

  /// <summary>
  /// Configures IOptions&lt;TOptions&gt;. DataAnnotations-based validation is enabled.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <param name="sectionKeyProvider"></param>
  /// <typeparam name="TOptions">Type of <see cref="Options"/></typeparam>
  /// <returns><see cref="IServiceCollection"/>></returns>
  public static IServiceCollection ConfigureValidatedOptions<TOptions>(
    this IServiceCollection services,
    IConfiguration configuration,
    Func<string> sectionKeyProvider)
        where TOptions : class, new()
  {
    var section = configuration.GetExistingSection(sectionKeyProvider());

    return services.ConfigureValidatedOptions<TOptions>(section);
  }

  /// <summary>
  /// Configures IOptions&lt;TOptions&gt;. delegate-based validation is enabled.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <param name="sectionKeyProvider"></param>
  /// <param name="validate"></param>
  /// <typeparam name="TOptions">Type of <see cref="Options"/></typeparam>
  /// <returns><see cref="IServiceCollection"/>></returns>
  public static IServiceCollection ConfigureValidatedOptions<TOptions>(
    this IServiceCollection services,
    IConfiguration configuration,
    Func<string> sectionKeyProvider,
    Func<TOptions, bool> validate)
      where TOptions : class, new()
  {
    var section = configuration.GetExistingSection(sectionKeyProvider());

    return services.ConfigureValidatedOptions<TOptions>(section, validate);
  }

  /// <summary>
  /// Configures IOptions&lt;TOptions&gt;. TOptionsValidator:IValidateOptions&lt;TOptions&gt;-based validation is enabled.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <param name="sectionKeyProvider"></param>
  /// <typeparam name="TOptions">Type of <see cref="Options"/></typeparam>
  /// <typeparam name="TValidator">Type of validator used to validate the options</typeparam>
  /// <returns><see cref="IServiceCollection"/>></returns>
  public static IServiceCollection ConfigureValidatedOptions<TOptions, TValidator>(
    this IServiceCollection services,
    IConfiguration configuration,
    Func<string> sectionKeyProvider)
      where TOptions : class, new()
      where TValidator : class, IValidator<TOptions>
  {
    var section = configuration.GetExistingSection(sectionKeyProvider());

    services
        .AddSingleton<IValidateOptions<TOptions>, FluentOptionsValidator<TOptions>>()
        .Configure<TOptions>(section);

    return services;
  }

  private static IServiceCollection ConfigureValidatedOptions<TOptions>(this IServiceCollection services, IConfigurationSection section) where TOptions : class, new()
  {
    _ = section ?? throw new ArgumentNullException(nameof(section));

    services
      .AddSingleton<IValidateOptions<TOptions>, MiniOptionsValidator<TOptions>>()
      .AddOptions<TOptions>()
      .Bind(section, DefaultBinderOptions);

    return services;
  }

  private static IServiceCollection ConfigureValidatedOptions<TOptions>(this IServiceCollection services, IConfigurationSection section, Func<TOptions, bool> validator) where TOptions : class, new()
  {
    _ = section ?? throw new ArgumentNullException(nameof(section));
    _ = validator ?? throw new ArgumentNullException(nameof(validator));

    services
      .AddOptions<TOptions>()
      .Bind(section, DefaultBinderOptions)
      .Validate(validator);

    return services;
  }
}