using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Ion.Configuration;

public static class ServiceCollectionExtensions
{
    public static TOptions ConfigureOptions<TOptions>(this IServiceCollection services, IConfiguration configuration,
        Func<string> sectionKeyProvider)
    where TOptions: class, new()
    {
        var options = new TOptions();
        configuration.GetExistingSection(sectionKeyProvider()).Bind(options);

        services.AddSingleton(options);

        return options;
    }

    public static IServiceCollection ConfigureValidatedOptions<TOptions>(this IServiceCollection services, IConfiguration configuration, Func<string> sectionKeyProvider)
        where TOptions : class, new()
    {
        var section = configuration.GetExistingSection(sectionKeyProvider());

        return services.ConfigureValidatedOptions<TOptions>(section);
    }

    public static IServiceCollection ConfigureValidatedOptions<TOptions, TOptionsValidator>(this IServiceCollection services, IConfiguration configuration, Func<string> sectionKeyProvider)
        where TOptions : class, new()
        where TOptionsValidator : class, IValidateOptions<TOptions>
    {
        var section = configuration.GetExistingSection(sectionKeyProvider());

        services
            .AddSingleton<IValidateOptions<TOptions>, TOptionsValidator>()
            .Configure<TOptions>(section);

        return services;
    }

    public static IServiceCollection ConfigureValidatedOptions<TOptions>(this IServiceCollection services, IConfiguration configuration, string name, Func<string> sectionKeyProvider)
        where TOptions : class, new()
    {
        var section = configuration.GetExistingSection(sectionKeyProvider());

        return services.ConfigureValidatedOptions<TOptions>(name, section);
    }

    public static IServiceCollection ConfigureValidatedOptions<TOptions>(this IServiceCollection services, IConfiguration config) where TOptions : class, new()
        => services.ConfigureValidatedOptions<TOptions>(global::Microsoft.Extensions.Options.Options.DefaultName, config);

    public static IServiceCollection ConfigureValidatedOptions<TOptions>(this IServiceCollection services, string name, IConfiguration config) where TOptions : class, new()
    {
        _ = config ?? throw new ArgumentNullException(nameof(config));
        
        services
            .AddOptions<TOptions>()
            .ValidateDataAnnotations()
            .Bind(config);

        return services;
    }

    public static IServiceCollection ConfigureValidatedOptions<TOptions>(this IServiceCollection services, Action<TOptions> configureOptions) where TOptions : class
        => services.ConfigureValidatedOptions<TOptions>(global::Microsoft.Extensions.Options.Options.DefaultName, configureOptions);

    public static IServiceCollection ConfigureValidatedOptions<TOptions>(this IServiceCollection services, string name, Action<TOptions> configureOptions) where TOptions : class
    {
        services.Configure(name, configureOptions);
        services.AddDataAnnotationValidatedOptions<TOptions>(name);
        return services;
    }

    private static IServiceCollection AddDataAnnotationValidatedOptions<TOptions>(this IServiceCollection services, string name) where TOptions : class
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TOptions>>(new DataAnnotationValidateOptions<TOptions>(name)));
        return services;
    }
}