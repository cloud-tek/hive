using Hive.HTTP.Authentication;
using Hive.HTTP.Resilience;

namespace Hive.HTTP;

public sealed class HiveHttpClientBuilder
{
  internal HttpClientRegistration Registration { get; }

  internal HiveHttpClientBuilder(HttpClientRegistration registration)
  {
    Registration = registration;
  }

  public HiveHttpClientBuilder Internal()
  {
    Registration.FluentFlavour = HttpClientFlavour.Internal;
    return this;
  }

  public HiveHttpClientBuilder External()
  {
    Registration.FluentFlavour = HttpClientFlavour.External;
    return this;
  }

  public HiveHttpClientBuilder WithBaseAddress(string baseAddress)
  {
    ArgumentNullException.ThrowIfNull(baseAddress);
    Registration.FluentBaseAddress = baseAddress;
    return this;
  }

  public HiveHttpClientBuilder WithAuthentication(Action<AuthenticationBuilder> configure)
  {
    ArgumentNullException.ThrowIfNull(configure);

    var builder = new AuthenticationBuilder();
    configure(builder);

    Registration.AuthenticationProviderFactory = builder.ProviderFactory;
    if (builder.AuthenticationType is not null)
      Registration.AuthenticationType = builder.AuthenticationType;

    return this;
  }

  public HiveHttpClientBuilder WithResilience(Action<ResilienceBuilder> configure)
  {
    ArgumentNullException.ThrowIfNull(configure);

    var builder = new ResilienceBuilder();
    configure(builder);

    Registration.FluentResilience = builder.Options;
    return this;
  }

  public HiveHttpClientBuilder WithHandler<THandler>() where THandler : DelegatingHandler
  {
    Registration.CustomHandlerTypes.Add(typeof(THandler));
    return this;
  }

  public HiveHttpClientBuilder WithRefitSettings(Refit.RefitSettings settings)
  {
    ArgumentNullException.ThrowIfNull(settings);
    Registration.RefitSettings = settings;
    return this;
  }
}