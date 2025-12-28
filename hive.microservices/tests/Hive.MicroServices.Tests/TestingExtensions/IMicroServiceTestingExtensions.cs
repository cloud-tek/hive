using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Hive.MicroServices;

/// <summary>
/// Testing extension methods for <see cref="IMicroService"/>
/// </summary>
public static class IMicroServiceTestingExtensions
{
  /// <summary>
  /// Prepares the microservice to use a TestServer-based external host.
  /// The host will be created during InitializeAsync with the configuration provided there.
  /// After calling this method, use microservice.InitializeAsync(config) to build and initialize the host,
  /// then microservice.StartAsync() to start it.
  /// </summary>
  /// <param name="microservice">The microservice instance</param>
  /// <returns>The microservice instance for fluent chaining</returns>
  /// <exception cref="ArgumentNullException">Thrown when microservice is null</exception>
  public static IMicroService UseTestHost(this IMicroService microservice)
  {
    _ = microservice ?? throw new ArgumentNullException(nameof(microservice));

    // Set a factory that will be called during InitializeAsync with the configuration
    return microservice.WithExternalHostFactory(config =>
    {
      var hostBuilder = new HostBuilder()
        .ConfigureWebHost(webHostBuilder =>
        {
          webHostBuilder.UseTestServer();
          microservice.ConfigureWebHost(webHostBuilder, config);
        });

      return hostBuilder.Build();
    });
  }
}
