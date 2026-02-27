using System.Reflection;
using Hive.Messaging.Configuration;
using Hive.Messaging.Telemetry;
using Hive.Messaging.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace Hive.Messaging;

internal abstract class MessagingExtensionBase<TSelf> : MicroServiceExtension<TSelf>, IActivitySourceProvider
  where TSelf : MessagingExtensionBase<TSelf>
{
  protected MessagingExtensionBase(IMicroServiceCore service) : base(service) { }

  public IEnumerable<string> ActivitySourceNames => ["Wolverine"];

  protected static void ConfigureWolverineCore(
    IServiceCollection svc,
    IConfiguration configuration,
    IMicroServiceCore microservice,
    Func<WolverineOptions, MessagingOptions, IMessagingTransportProvider?> applyBuilder)
  {
    var messagingSection = configuration.GetSection(MessagingOptions.SectionKey);
    var options = new MessagingOptions();
    if (messagingSection.Exists())
      messagingSection.Bind(options);

    svc.AddWolverine(opts =>
    {
      opts.ServiceName = microservice.Name;

      // AddWolverine is called from inside Hive.Messaging, so Wolverine
      // incorrectly identifies this library as the application assembly.
      // Point it at the actual entry assembly for handler discovery.
      var entryAssembly = Assembly.GetEntryAssembly();
      if (entryAssembly != null)
      {
        opts.ApplicationAssembly = entryAssembly;
      }

      // Provider set explicitly by UseRabbitMq() or similar on the builder
      var provider = applyBuilder(opts, options);

      // Apply transport configuration
      provider?.ConfigureTransport(opts, options, configuration);
    });

    svc.Decorate<IMessageBus, TelemetryMessageBus>();
    svc.AddSingleton<WolverineSendActivityListener>();
    svc.AddHostedService(sp => sp.GetRequiredService<WolverineSendActivityListener>());
  }
}