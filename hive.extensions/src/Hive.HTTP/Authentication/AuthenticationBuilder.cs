namespace Hive.HTTP.Authentication;

/// <summary>
/// Fluent builder for configuring HTTP client authentication.
/// </summary>
public sealed class AuthenticationBuilder
{
  internal Func<IServiceProvider, IAuthenticationProvider>? ProviderFactory { get; private set; }

  internal string? AuthenticationType { get; private set; }

  /// <summary>
  /// Configures bearer token authentication using a factory that produces tokens per-request.
  /// </summary>
  /// <param name="tokenFactoryProvider">
  /// A delegate that receives an <see cref="IServiceProvider"/> and returns a token factory function.
  /// The token factory is invoked on each request to obtain a fresh token.
  /// </param>
  /// <returns>The builder instance for chaining.</returns>
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

  /// <summary>
  /// Configures static API key authentication using a custom header.
  /// </summary>
  /// <param name="headerName">The HTTP header name to set.</param>
  /// <param name="value">The API key value.</param>
  /// <returns>The builder instance for chaining.</returns>
  public AuthenticationBuilder ApiKey(string headerName, string value)
  {
    ArgumentNullException.ThrowIfNull(headerName);
    ArgumentNullException.ThrowIfNull(value);

    AuthenticationType = "ApiKey";
    ProviderFactory = _ => new ApiKeyProvider(headerName, value);
    return this;
  }

  /// <summary>
  /// Configures a custom authentication provider resolved from the service provider.
  /// </summary>
  /// <param name="factory">A delegate that creates the authentication provider from the service provider.</param>
  /// <returns>The builder instance for chaining.</returns>
  public AuthenticationBuilder Custom(Func<IServiceProvider, IAuthenticationProvider> factory)
  {
    ArgumentNullException.ThrowIfNull(factory);

    AuthenticationType = "Custom";
    ProviderFactory = factory;
    return this;
  }
}