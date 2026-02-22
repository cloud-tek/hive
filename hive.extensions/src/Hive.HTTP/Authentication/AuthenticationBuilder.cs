namespace Hive.HTTP.Authentication;

public sealed class AuthenticationBuilder
{
  internal Func<IServiceProvider, IAuthenticationProvider>? ProviderFactory { get; private set; }

  internal string? AuthenticationType { get; private set; }

  public AuthenticationBuilder BearerToken(
    Func<IServiceProvider, Func<CancellationToken, Task<string>>> tokenFactoryProvider)
  {
    ArgumentNullException.ThrowIfNull(tokenFactoryProvider);

    AuthenticationType = "BearerToken";
    ProviderFactory = sp =>
    {
      var tokenFactory = tokenFactoryProvider(sp);
      return new BearerTokenProvider(tokenFactory);
    };
    return this;
  }

  public AuthenticationBuilder ApiKey(string headerName, string value)
  {
    ArgumentNullException.ThrowIfNull(headerName);
    ArgumentNullException.ThrowIfNull(value);

    AuthenticationType = "ApiKey";
    ProviderFactory = _ => new ApiKeyProvider(headerName, value);
    return this;
  }

  public AuthenticationBuilder Custom(Func<IServiceProvider, IAuthenticationProvider> factory)
  {
    ArgumentNullException.ThrowIfNull(factory);

    AuthenticationType = "Custom";
    ProviderFactory = factory;
    return this;
  }
}