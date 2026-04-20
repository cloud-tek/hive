namespace Hive.HTTP.Authentication;

internal sealed class AuthenticationHandler : DelegatingHandler
{
  private readonly IAuthenticationProvider _provider;

  public AuthenticationHandler(IAuthenticationProvider provider)
  {
    _provider = provider ?? throw new ArgumentNullException(nameof(provider));
  }

  protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken)
  {
    await _provider.ApplyAsync(request, cancellationToken);
    return await base.SendAsync(request, cancellationToken);
  }
}