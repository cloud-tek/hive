using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hive.Configuration.Validation;

public class FluentOptionsValidator<TOptions> : IValidateOptions<TOptions> where TOptions : class
{
  private readonly IServiceProvider _serviceProvider;

  public FluentOptionsValidator(IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
  }

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
