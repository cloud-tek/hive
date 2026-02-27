using Hive.MicroServices.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.HealthChecks;

/// <summary>
/// Hive extension that registers health check infrastructure:
/// registry, startup service, and background evaluation service.
/// </summary>
internal sealed class HealthChecksExtension : MicroServiceExtension<HealthChecksExtension>, IActivitySourceProvider
{
  private readonly HealthChecksBuilder _builder;

  public HealthChecksExtension(IMicroServiceCore service, HealthChecksBuilder builder) : base(service)
  {
    _builder = builder;
  }

  public IEnumerable<string> ActivitySourceNames => [HealthCheckActivitySource.Name];

  public override IServiceCollection ConfigureServices(IServiceCollection services, IMicroServiceCore microservice)
  {
    ConfigureActions.Add((svc, cfg) =>
    {
      // Three-tier priority: POCO defaults (30s) < IConfiguration < Builder (explicit)
      var globalOptions = new HealthChecksOptions();
      var section = cfg.GetSection(HealthChecksOptions.SectionKey);
      if (section.Exists())
      {
        if (section[nameof(HealthChecksOptions.Interval)] is { } intervalStr && int.TryParse(intervalStr, out var intervalSecs))
          globalOptions.Interval = TimeSpan.FromSeconds(intervalSecs);
      }

      if (_builder.Interval.HasValue)
        globalOptions.Interval = _builder.Interval.Value;

      // Register explicitly configured health checks in DI
      var explicitRegistrations = _builder.GetRegistrations();
      foreach (var (checkType, _) in explicitRegistrations)
      {
        svc.AddSingleton(typeof(HiveHealthCheck), checkType);
      }

      // Register the registry as singleton
      svc.AddSingleton<HealthCheckRegistry>();
      svc.AddSingleton<IHealthCheckStateProvider>(sp => sp.GetRequiredService<HealthCheckRegistry>());

      // Register startup and background services
      svc.AddHostedStartupService<HealthCheckStartupService>();
      svc.AddHostedService<HealthCheckBackgroundService>();

      // Store configuration in DI for the services to resolve
      svc.AddSingleton(new HealthCheckConfiguration(
        globalOptions,
        explicitRegistrations,
        cfg));
    });

    return services;
  }
}

/// <summary>
/// Internal configuration bag passed to startup and background services via DI.
/// </summary>
internal sealed class HealthCheckConfiguration
{
  public HealthChecksOptions GlobalOptions { get; }
  public IReadOnlyDictionary<Type, HiveHealthCheckOptions> ExplicitRegistrations { get; }
  public IConfiguration Configuration { get; }

  public HealthCheckConfiguration(
    HealthChecksOptions globalOptions,
    IReadOnlyDictionary<Type, HiveHealthCheckOptions> explicitRegistrations,
    IConfiguration configuration)
  {
    GlobalOptions = globalOptions;
    ExplicitRegistrations = explicitRegistrations;
    Configuration = configuration;
  }
}
