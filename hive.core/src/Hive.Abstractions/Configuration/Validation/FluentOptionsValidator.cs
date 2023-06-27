using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hive.Configuration.Validation;

public class FluentOptionsValidator<TOptions> : IValidateOptions<TOptions> where TOptions : class
{
  private readonly IServiceProvider _serviceProvider;
  private readonly string? _name;

  public FluentOptionsValidator(string? name, IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    _name = name;
  }

  public ValidateOptionsResult Validate(string? name, TOptions options)
  {
    // Null name is used to configure all named options.
    if (_name != null && _name != name)
    {
      // Ignored if not validating this instance.
      return ValidateOptionsResult.Skip;
    }

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
      errors.Add($"Validation failed for '{typeName}.{result.PropertyName}' with the error: '{result.ErrorMessage}'.");
    }

    return ValidateOptionsResult.Fail(errors);
  }
}
