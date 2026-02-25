using FluentValidation;
using FluentValidation.Results;
using Hive.Messaging.Configuration;
using Hive.Messaging.Middleware;
using Hive.Messaging.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.ErrorHandling;

namespace Hive.Messaging;

internal sealed class MessagingExtension : MessagingExtensionBase<MessagingExtension>
{
  private readonly Action<HiveMessagingBuilder> _configure;

  public MessagingExtension(IMicroServiceCore service, Action<HiveMessagingBuilder> configure)
    : base(service) => _configure = configure;

  public override IServiceCollection ConfigureServices(
    IServiceCollection services,
    IMicroServiceCore microservice)
  {
    ConfigureActions.Add((svc, configuration) =>
    {
      ConfigureWolverineCore(svc, configuration, microservice, (opts, options) =>
      {
        var builder = new HiveMessagingBuilder(opts, options);
        _configure(builder);
        builder.ApplyEscapeHatch();

        // Transport provider set explicitly by UseRabbitMq() or similar
        var provider = builder.TransportProvider;

        // Apply deferred registrations (WithHandling, WithSending) with resolved provider
        builder.ApplyDeferredRegistrations(provider);

        // Validate via provider
        ValidateTransport(provider, options, configuration);

        // Middleware
        opts.Policies.AddMiddleware(typeof(ReadinessGateMiddleware));
        opts.Policies.AddMiddleware<MessagingHandlerMiddleware>();

        opts.Policies.OnException<ServiceNotReadyException>()
          .PauseThenRequeue(TimeSpan.FromSeconds(5));

        return provider;
      });
    });

    return services;
  }

  private static void ValidateTransport(
    IMessagingTransportProvider? provider,
    MessagingOptions options,
    IConfiguration configuration)
  {
    if (provider == null)
      return;

    var errors = provider.Validate(options, configuration).ToList();
    if (errors.Count > 0)
      throw new ValidationException(
        errors.Select(e => new ValidationFailure("", e)));
  }
}