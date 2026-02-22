namespace Hive.HTTP.Authentication;

internal sealed class BearerTokenProvider : IAuthenticationProvider
{
  private readonly Func<CancellationToken, Task<string>> _tokenFactory;

  public BearerTokenProvider(Func<CancellationToken, Task<string>> tokenFactory)
  {
    _tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
  }

  public async Task ApplyAsync(HttpRequestMessage message, CancellationToken cancellationToken)
  {
    var token = await _tokenFactory(cancellationToken);
    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
  }
}