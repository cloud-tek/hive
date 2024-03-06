using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hive.Configuration.Validation;

/// <summary>
/// A fluent options validator that uses <see cref="IValidator{T}"/> to validate options
/// </summary>
/// <typeparam name="TOptions">Type of <see cref="Options"/></typeparam>
public class FluentOptionsValidator<TOptions> : IValidateOptions<TOptions> where TOptions : class
{
  private readonly IServiceProvider _serviceProvider;

  /// <summary>
  /// Creates a new instance of <see cref="FluentOptionsValidator{TOptions}"/>
  /// </summary>
  /// <param name="serviceProvider"></param>
  /// <exception cref="ArgumentNullException">Thrown when the serviceProvider is null</exception>
  public FluentOptionsValidator(IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
  }

  /// <summary>
  /// Validates the provided options instance
  /// </summary>
  /// <param name="name"></param>
  /// <param name="options"></param>
  /// <returns><see cref="ValidateOptionsResult"/></returns>
  /// <exception cref="ArgumentNullException">Thrown when options are null</exception>
  public ValidateOptionsResult Validate(string? name, TOptions options)
  {
    _ = options ?? throw new ArgumentNullException(nameof(options));

    using var scope = _serviceProvider.CreateScope();
    var validator = scope.ServiceProvider.GetRequiredService<IValidator<TOptions>>();
    var results = validator.Validate(options);
    if (results.IsValid)
    {
      return ValidateOptionsResult.Success;
    }

    var typeName = options.GetType().Name;
    var errors = new List<string>();
    foreach (var result in results.Errors)
    {
      errors.Add($"{typeName}.{result.PropertyName} : {result.ErrorMessage}");
    }

    return ValidateOptionsResult.Fail(errors);
  }
}