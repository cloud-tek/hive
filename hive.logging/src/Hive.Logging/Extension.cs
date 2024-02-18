using Hive.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Extensions.Hosting;
using ILogger = Serilog.ILogger;

namespace Hive.Logging;

internal sealed class Extension : MicroServiceExtension, IHaveRequestLoggingMiddleware
{
  private readonly Action<LoggingConfigurationBuilder> action;

  internal Extension(IMicroService service, Action<LoggingConfigurationBuilder> action)
      : base(service)
  {
    this.action = action ?? throw new ArgumentNullException(nameof(action));

    ConfigureActions.Add((svc, configuration) =>
    {
      var options = svc.PreConfigureOptions<Options>(configuration, () => Options.SectionKey);

      var builder = new LoggingConfigurationBuilder(this);
      action(builder);

      ILogger logger = new LoggerConfiguration()
              .ConfigureSerilog(service, svc, options, builder)
              .CreateLogger();

      ILoggerFactory loggerFactory;
      if (service.ExternalLogger)
      {
        loggerFactory = new NullLoggerFactory();
      }
      else
      {
        loggerFactory = new Serilog.Extensions.Logging.SerilogLoggerFactory(logger, true);
      }

      svc.AddSingleton<ILoggerFactory>((Func<IServiceProvider, ILoggerFactory>)(provider => loggerFactory));
      svc.AddSingleton<ILogger>(logger);
      var implementationInstance = new DiagnosticContext(logger);
      svc.AddSingleton<DiagnosticContext>(new DiagnosticContext(logger));
      svc.AddSingleton<IDiagnosticContext>((IDiagnosticContext)implementationInstance);
      svc.AddSingleton(new RequestLoggingOptions());

      var microservice = (MicroServiceBase)service;
      microservice.Logger = loggerFactory.CreateLogger<IMicroService>();
    });
  }

#pragma warning disable SA1500, SA1513
  public Action<IApplicationBuilder> ConfigureRequestLoggingMiddleware
  {
    get;
    internal set;
  } = default!;
#pragma warning restore SA1500, SA1513
}