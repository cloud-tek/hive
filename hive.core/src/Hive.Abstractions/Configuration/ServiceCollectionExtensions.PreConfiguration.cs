using System.Text;
using FluentValidation;
using Hive.Exceptions;
using Hive.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MiniValidation;

namespace Hive.Configuration;

/// <summary>
/// All extensions in this file make the configuration accessible before <seealso href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.servicecollectioncontainerbuilderextensions.buildserviceprovider?view=dotnet-plat-ext-7.0">IServiceCollection.BuildServiceProvider()</seealso>
/// is called
/// </summary>
public static partial class ServiceCollectionExtensions
{
  public static IOptions<TOptions> PreConfigureOptions<TOptions>(this IServiceCollection services,
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

  public static IOptions<TOptions> PreConfigureValidatedOptions<TOptions>(this IServiceCollection services,
    IConfiguration configuration, Func<string> sectionKeyProvider)
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

  public static IOptions<TOptions> PreConfigureValidatedOptions<TOptions>(this IServiceCollection services,
    IConfiguration configuration, Func<string> sectionKeyProvider, Func<TOptions, bool> validate)
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
        new[] { "Options validation failed" });
    }

    var optionsInstance = Options.Create(options);
    services.AddSingleton(optionsInstance);

    return optionsInstance;
  }

  public static IOptions<TOptions> PreConfigureValidatedOptions<TOptions, TValidator>(
    this IServiceCollection services, IConfiguration configuration,
    Func<string> sectionKeyProvider)
    where TOptions : class, new()
    where TValidator : class, IValidator<TOptions>, new()
  {
    _ = services ?? throw new ArgumentNullException(nameof(services));
    _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _ = sectionKeyProvider ?? throw new ArgumentNullException(nameof(sectionKeyProvider));

    var options = new TOptions();
    var key = sectionKeyProvider();

    configuration.GetExistingSection(key)
      .Bind(options);

    var validator = new TValidator();
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
}
