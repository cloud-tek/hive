using System.Reflection;
using Hive.Extensions;
using Hive.MicroServices.Middleware;
using Hive.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hive.MicroServices;

/// <summary>
/// Extension methods for <see cref="IMicroService"/>
/// </summary>
public static class IMicroServiceExtensions
{
  /// <summary>
  /// Add a configuration action to the service
  /// </summary>
  /// <param name="microservice"></param>
  /// <param name="action"></param>
  /// <returns><see cref="IMicroService"/></returns>
  /// <exception cref="ArgumentNullException">Thrown when any of the provided arguments are null</exception>
  public static IMicroService ConfigureServices(this IMicroService microservice, Action<IServiceCollection, IConfiguration> action)
  {
    _ = microservice ?? throw new ArgumentNullException(nameof(microservice));
    _ = action ?? throw new ArgumentNullException(nameof(action));

    var service = (MicroService)microservice;

    service.ConfigureActions.Add(action);

    if (service.Environment.IsDevelopment())
    {
      service.ConfigureActions.Add((svc, cfg) =>
      {
        svc.AddMiddlewareAnalysis();
      });
    }

    return microservice;
  }

  /// <summary>
  /// Override the services' entrypoint for testing purposes
  /// </summary>
  /// <typeparam name="T">Type of the test class containing the test code</typeparam>
  /// <param name="microservice">Service under test</param>
  /// <returns><see cref="IMicroService"/></returns>
  public static IMicroService InTestClass<T>(this IMicroService microservice)
      where T : class
  {
    return microservice.InTestAssembly(typeof(T).Assembly);
  }

  /// <summary>
  /// Override the services' entrypoint for testing purposes
  /// </summary>
  /// <param name="microservice">Service under test</param>
  /// <param name="assembly">Assembly containing the test code</param>
  /// <returns><see cref="IMicroService"/></returns>
  /// <exception cref="ArgumentNullException">Thrown when any of the provided arguments are null</exception>
  public static IMicroService InTestAssembly(this IMicroService microservice, Assembly assembly)
  {
    _ = microservice ?? throw new ArgumentNullException(nameof(microservice));
    _ = assembly ?? throw new ArgumentNullException(nameof(assembly));

    var service = (MicroService)microservice;
    service.MicroServiceEntrypointAssemblyProvider = () => assembly;

    return microservice;
  }

  /// <summary>
  /// Configures an IWebHostBuilder with the microservice's service registrations and pipeline configuration.
  /// Use this with TestServer for integration testing while keeping all Hive configuration.
  /// </summary>
  /// <param name="microservice">The microservice instance</param>
  /// <param name="webHostBuilder">The web host builder to configure</param>
  /// <param name="configuration">Optional configuration to use (defaults to empty configuration)</param>
  /// <returns>The configured <see cref="IWebHostBuilder"/></returns>
  /// <exception cref="ArgumentNullException">Thrown when microservice or webHostBuilder is null</exception>
  public static IWebHostBuilder ConfigureWebHost(this IMicroService microservice, IWebHostBuilder webHostBuilder, IConfigurationRoot? configuration = null)
  {
    _ = microservice ?? throw new ArgumentNullException(nameof(microservice));
    _ = webHostBuilder ?? throw new ArgumentNullException(nameof(webHostBuilder));

    var service = (MicroService)microservice;
    var config = configuration ?? new ConfigurationBuilder().Build();

    webHostBuilder
      .ConfigureServices((ctx, services) =>
      {
        // Register the microservice instance so StartupService can access it
        services.AddSingleton<IMicroService>(microservice);
        services.AddSingleton<IConfigurationRoot>(config);
        services.AddSingleton<IConfiguration>(config);

        // Add routing services (required by UseRouting/UseEndpoints middleware)
        services.AddRouting();

        // Run all service configuration actions
        service.ConfigureActions.ForEach(action => action(services, config));
        service.Extensions.ForEach(extension =>
          extension.ConfigureActions.ForEach(action => action(services, config)));
      })
      .Configure(app =>
      {
        // Configure request logging middleware if present
        if (service.Extensions.SingleOrDefault(ex => ex is IHaveRequestLoggingMiddleware) is IHaveRequestLoggingMiddleware lex
            && lex.ConfigureRequestLoggingMiddleware != null)
        {
          lex.ConfigureRequestLoggingMiddleware(app);
        }

        // Run all pipeline configuration actions
        service.ConfigurePipelineActions.ForEach(action => action(app));
      });

    return webHostBuilder;
  }

  internal static IMicroService UseCoreMicroServicePipeline(this IMicroService microservice, Action<IApplicationBuilder>? developmentOnlyPipeline = null)
  {
    var service = (MicroService)microservice;

    service.ConfigurePipelineActions.Add(app =>
    {
      app.UseMiddleware<LivenessMiddleware>();
    });

    if (developmentOnlyPipeline != null && service.Environment.IsDevelopment())
    {
      service.ConfigurePipelineActions.Add(developmentOnlyPipeline);
    }

    service.ConfigurePipelineActions.Add(MicroService.Middleware.MicroServiceLifecycleMiddlewares);

    service.ConfigurePipelineActions.Add(app =>
    {
      app.UseMiddleware<TracingMiddleware>();
    });

    return microservice;
  }

  internal static IMicroService ValidatePipelineModeNotSet(this IMicroService microservice)
  {
    var service = (MicroService)microservice;

    if (service.PipelineMode != MicroServicePipelineMode.NotSet)
    {
      throw new InvalidOperationException($"MicroService {nameof(service.PipelineMode)} is already set");
    }

    return microservice;
  }

  /// <summary>
  /// Configures the default service pipeline
  /// </summary>
  /// <param name="microservice"></param>
  /// <returns><see cref="IMicroService"/></returns>
  public static IMicroService ConfigureDefaultServicePipeline(this IMicroService microservice)
  {
    var service = (MicroService)microservice;

    service.ValidatePipelineModeNotSet();

    service.ConfigureActions.Add(MicroService.Services.LifecycleServices);

    service.UseCoreMicroServicePipeline();
    service
        .ConfigureExtensions()
        .ConfigurePipelineActions.Add(app =>
        {
          app.UseRouting();

          // Apply CORS middleware (uses default policy configured in Extension)
          var corsExtension = microservice.Extensions.SingleOrDefault(x => x.Is<CORS.Extension>()) as CORS.Extension;
          if (corsExtension != null)
          {
            app.UseCors();
          }

          app.UseAuthorization();
          app.UseEndpoints(endpoints =>
              {
                endpoints.MapGet("/*", (ctx) =>
                    {
                      ctx.Response.StatusCode = 404;
                      return Task.CompletedTask;
                    });
              });
        });

    service.PipelineMode = MicroServicePipelineMode.None;

    return service;
  }
}