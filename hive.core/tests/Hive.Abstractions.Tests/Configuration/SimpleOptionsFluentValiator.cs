using System;
using FluentValidation;

namespace Hive.Tests.Configuration;

public class SimpleOptionsFluentValiator : AbstractValidator<SimpleOptions>
{
  public SimpleOptionsFluentValiator()
  {
    RuleFor(x => x.Name).NotEmpty().MinimumLength(3);
    RuleFor(x => x.Address)
      .NotEmpty()
      .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
      .When(x => !string.IsNullOrEmpty(x.Address));
  }
}
