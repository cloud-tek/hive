using FluentValidation;
using Hive.MicroServices.Lifecycle;

namespace Hive.HTTP.Lifecycle;

internal sealed class HttpClientValidationStartupService : IHostedStartupService
{
  private readonly IReadOnlyList<ValidationException> _failures;

  public HttpClientValidationStartupService(IReadOnlyList<ValidationException> failures)
  {
    _failures = failures;
  }

  public bool Completed { get; private set; }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    if (_failures.Count > 0)
    {
      throw new AggregateException(
        "HTTP client configuration validation failed",
        _failures);
    }

    Completed = true;
    return Task.CompletedTask;
  }
}