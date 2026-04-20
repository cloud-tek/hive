using Hive.MicroServices.Extensions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Hive.MicroServices.Testing;

/// <summary>
/// Testing extension methods for <see cref="IMicroService"/>
/// </summary>
public static class IMicroServiceTestingExtensions
{
  /// <summary>
  /// Configures the microservice to use a TestServer-based external host for integration testing.
  /// The host will be created during InitializeAsync with the configuration provided there.
  /// After calling this method, use microservice.InitializeAsync(config) to build and initialize the host,
  /// then microservice.StartAsync() to start it.
  /// This method is idempotent - calling it multiple times has the same effect as calling it once.
  /// </summary>
  /// <param name="microservice">The microservice instance</param>
  /// <returns>The microservice instance for fluent chaining</returns>
  /// <exception cref="ArgumentNullException">Thrown when microservice is null</exception>
  public static IMicroService ConfigureTestHost(this IMicroService microservice)
  {
    _ = microservice ?? throw new ArgumentNullException(nameof(microservice));

    var service = (MicroService)microservice;

    // Idempotent: return early if already configured
    if (service.ExternalHostFactory != null)
    {
      return microservice;
    }

    service.ExternalHostFactory = config =>
    {
      var hostBuilder = new HostBuilder()
        .ConfigureWebHost(webHostBuilder =>
        {
          webHostBuilder.UseTestServer();
          microservice.ConfigureWebHost(webHostBuilder, config);
        });

      return hostBuilder.Build();
    };

    return microservice;
  }
}