using Hive.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Hive.Configuration;

/// <summary>
/// All extensions in this file make the configuration accessible before <seealso href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.servicecollectioncontainerbuilderextensions.buildserviceprovider?view=dotnet-plat-ext-7.0">IServiceCollection.BuildServiceProvider()</seealso>
/// is called
/// </summary>
public static partial class ServiceCollectionExtensions
{

    public static IOptions<TOptions> PreConfigureOptions<TOptions>(this IServiceCollection services, IConfiguration configuration,
        Func<string> sectionKeyProvider)
    where TOptions: class, new()
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
      where TOptions: class, new()
    {
      _ = services ?? throw new ArgumentNullException(nameof(services));
      _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
      _ = sectionKeyProvider ?? throw new ArgumentNullException(nameof(sectionKeyProvider));

      var options = new TOptions();
      var key = sectionKeyProvider();

      configuration.GetExistingSection(key)
        .Bind(options);

      var validateOptions = new DataAnnotationValidateOptions<TOptions>(Options.DefaultName);
      var result = validateOptions.Validate(Options.DefaultName, options);

      if (result.Failed)
      {
        throw new ConfigurationException(result.FailureMessage ?? "Validation failed");
      }

      var optionsInstance = Options.Create(options);
      services.AddSingleton(optionsInstance);

      return optionsInstance;
    }

    public static IOptions<TOptions> PreConfigureValidatedOptions<TOptions>(this IServiceCollection services,
      IConfiguration configuration, Func<string> sectionKeyProvider, Func<TOptions, bool> validate)
      where TOptions: class, new()
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
        throw new ConfigurationException("Validation failed");
      }

      var optionsInstance = Options.Create(options);
      services.AddSingleton(optionsInstance);

      return optionsInstance;
    }

    public static IOptions<TOptions> PreConfigureValidatedOptions<TOptions, TOptionsValidator>(this IServiceCollection services, IConfiguration configuration,
      Func<string> sectionKeyProvider)
      where TOptions: class, new()
      where TOptionsValidator : class, IValidateOptions<TOptions>, new()
    {
      _ = services ?? throw new ArgumentNullException(nameof(services));
      _ = configuration ?? throw new ArgumentNullException(nameof(configuration));
      _ = sectionKeyProvider ?? throw new ArgumentNullException(nameof(sectionKeyProvider));

      var options = new TOptions();
      var key = sectionKeyProvider();

      configuration.GetExistingSection(key)
        .Bind(options);

      var validator = new TOptionsValidator();
      var result = validator.Validate(Options.DefaultName, options);

      if (result.Failed)
      {
        throw new ConfigurationException($"{key} : {result.FailureMessage}");
      }

      var optionsInstance = Options.Create(options);
      services.AddSingleton(optionsInstance);

      return optionsInstance;
    }
}
