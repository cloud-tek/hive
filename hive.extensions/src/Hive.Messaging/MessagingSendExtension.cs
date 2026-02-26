using Hive.Messaging.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hive.Messaging;

internal sealed class MessagingSendExtension : MessagingExtensionBase<MessagingSendExtension>
{
  private readonly Action<HiveMessagingSendBuilder> _configure;

  public MessagingSendExtension(IMicroServiceCore service, Action<HiveMessagingSendBuilder> configure)
    : base(service) => _configure = configure;

  public override IServiceCollection ConfigureServices(
    IServiceCollection services,
    IMicroServiceCore microservice)
  {
    ConfigureActions.Add((svc, configuration) =>
    {
      ConfigureWolverineCore(svc, configuration, microservice, (opts, options) =>
      {
        var builder = new HiveMessagingSendBuilder(opts, options);
        _configure(builder);
        builder.ApplyEscapeHatch();

        // Transport provider set explicitly by UseRabbitMq() or similar
        var provider = builder.TransportProvider;

        // Apply deferred registrations (WithSending) with resolved provider
        builder.ApplyDeferredRegistrations(provider);

        // Validate via provider
        if (provider != null)
        {
          var errors = provider.Validate(options, configuration).ToList();
          if (errors.Count > 0)
            throw new OptionsValidationException(
              MessagingOptions.SectionKey, typeof(MessagingOptions), errors);
        }

        return provider;
      });
    });

    return services;
  }
}