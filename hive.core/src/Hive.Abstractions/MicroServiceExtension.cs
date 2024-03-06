using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hive;

/// <summary>
/// The base class for all microservice extensions
/// </summary>
public abstract class MicroServiceExtension
{
  /// <summary>
  /// Initializes a new instance of the <see cref="MicroServiceExtension"/> class
  /// </summary>
  /// <param name="service"></param>
  /// <exception cref="ArgumentNullException">Thrown when provided IMicroservice is null</exception>
  protected MicroServiceExtension(IMicroService service)
  {
    Service = service ?? throw new ArgumentNullException(nameof(service));
  }

  /// <summary>
  /// The list of actions to perform when configuring the IServiceCollection
  /// </summary>
  public IList<Action<IServiceCollection, IConfiguration>> ConfigureActions { get; } = new List<Action<IServiceCollection, IConfiguration>>();

  /// <summary>
  /// The IMicroService reference to the owning microservice
  /// </summary>
  protected IMicroService Service { get; init; }

  /// <summary>
  /// Virtual method to override in order to configure custom middleware with the IApplicationBuilder
  /// </summary>
  /// <param name="app"></param>
  /// <param name="microservice"></param>
  /// <returns>The IApplicationBuilder</returns>
  public virtual IApplicationBuilder Configure(IApplicationBuilder app, IMicroService microservice)
  {
    return app;
  }

  /// <summary>
  /// Virtual method to override in order to configure custom middleware with the IApplicationBuilder, which are injected before the readiness probe
  /// </summary>
  /// <param name="app"></param>
  /// <param name="env"></param>
  /// <returns>The IApplicationBuilder</returns>
  public virtual IApplicationBuilder ConfigureBeforeReadinessProbe(IApplicationBuilder app, IWebHostEnvironment env)
  {
    return app;
  }

  /// <summary>
  /// Virtual method to override in order to configure custom endpoints with the IEndpointRouteBuilder
  /// </summary>
  /// <param name="builder"></param>
  /// <returns>The IEndpointRouteBuilder</returns>
  public virtual IEndpointRouteBuilder ConfigureEndpoints(IEndpointRouteBuilder builder)
  {
    return builder;
  }

  /// <summary>
  /// Virtual method to override in order to configure custom healthchecks with the IHealthChecksBuilder
  /// </summary>
  /// <param name="builder"></param>
  /// <returns>The IHealthChecksBuilder</returns>
  public virtual IHealthChecksBuilder ConfigureHealthChecks(IHealthChecksBuilder builder)
  {
    return builder;
  }

  /// <summary>
  /// Virtual method to override in order to configure the IServiceCollection for the IMicroservice
  /// </summary>
  /// <param name="services"></param>
  /// <param name="microservice"></param>
  /// <returns>The IServiceCollection</returns>
  public virtual IServiceCollection ConfigureServices(IServiceCollection services, IMicroService microservice)
  {
    return services;
  }
}

/// <summary>
/// Helper extensions for the MicroServiceExtension(s)
/// </summary>
public static class MicroServiceExtensionMethods
{
  /// <summary>
  /// Extension method to check if the MicroServiceExtension is of a specific type
  /// </summary>
  /// <typeparam name="TExtension">Type of the extension</typeparam>
  /// <param name="extension"></param>
  /// <returns>boolean</returns>
  public static bool Is<TExtension>(this MicroServiceExtension extension)
    where TExtension : MicroServiceExtension
  {
    return extension.GetType() == typeof(TExtension);
  }
}