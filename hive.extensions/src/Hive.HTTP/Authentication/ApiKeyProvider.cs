namespace Hive.HTTP.Authentication;

internal sealed class ApiKeyProvider : IAuthenticationProvider
{
  private readonly string _headerName;
  private readonly string _value;

  public ApiKeyProvider(string headerName, string value)
  {
    _headerName = headerName ?? throw new ArgumentNullException(nameof(headerName));
    _value = value ?? throw new ArgumentNullException(nameof(value));
  }

  public Task ApplyAsync(HttpRequestMessage message, CancellationToken cancellationToken)
  {
    message.Headers.TryAddWithoutValidation(_headerName, _value);
    return Task.CompletedTask;
  }
}