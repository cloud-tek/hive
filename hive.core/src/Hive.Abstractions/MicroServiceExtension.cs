using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hive;

/// <summary>
/// Non-generic base class for all microservice extensions.
/// Used for the Extensions collection to reference extensions polymorphically.
/// </summary>
public abstract class MicroServiceExtension
{
  /// <summary>
  /// Initializes a new instance of the <see cref="MicroServiceExtension"/> class
  /// </summary>
  /// <param name="service"></param>
  /// <exception cref="ArgumentNullException">Thrown when provided IMicroServiceCore is null</exception>
  protected MicroServiceExtension(IMicroServiceCore service)
  {
    Service = service ?? throw new ArgumentNullException(nameof(service));
  }

  /// <summary>
  /// The list of actions to perform when configuring the IServiceCollection
  /// </summary>
  public IList<Action<IServiceCollection, IConfiguration>> ConfigureActions { get; } = new List<Action<IServiceCollection, IConfiguration>>();

  /// <summary>
  /// The IMicroServiceCore reference to the owning service
  /// </summary>
  protected IMicroServiceCore Service { get; init; }

  /// <summary>
  /// Virtual method to override in order to configure custom middleware with the IApplicationBuilder
  /// </summary>
  /// <param name="app"></param>
  /// <param name="microservice"></param>
  /// <returns>The IApplicationBuilder</returns>
  public virtual IApplicationBuilder Configure(IApplicationBuilder app, IMicroServiceCore microservice)
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
  /// Virtual method to override in order to configure the IServiceCollection for the service
  /// </summary>
  /// <param name="services"></param>
  /// <param name="microservice"></param>
  /// <returns>The IServiceCollection</returns>
  public virtual IServiceCollection ConfigureServices(IServiceCollection services, IMicroServiceCore microservice)
  {
    return services;
  }
}

/// <summary>
/// Generic base class for microservice extensions with compile-time factory enforcement
/// </summary>
/// <typeparam name="TExtension">The derived extension type</typeparam>
public abstract class MicroServiceExtension<TExtension> : MicroServiceExtension, IMicroServiceExtension<TExtension>
  where TExtension : MicroServiceExtension<TExtension>
{
  /// <summary>
  /// Initializes a new instance of the <see cref="MicroServiceExtension{TExtension}"/> class
  /// </summary>
  /// <param name="service"></param>
  protected MicroServiceExtension(IMicroServiceCore service) : base(service)
  {
  }

  /// <summary>
  /// Default factory implementation using Activator.CreateInstance.
  /// Derived types can override by providing their own static Create implementation.
  /// </summary>
  /// <param name="service">The microservice core instance</param>
  /// <returns>A new instance of the extension</returns>
#pragma warning disable CA1000 // Do not declare static members on generic types - Required for IMicroServiceExtension interface
  public static TExtension Create(IMicroServiceCore service)
  {
    return (TExtension)Activator.CreateInstance(typeof(TExtension), service)!
           ?? throw new InvalidOperationException(
             $"Failed to create instance of extension {typeof(TExtension).Name}");
  }
#pragma warning restore CA1000
}