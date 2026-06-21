using Microsoft.AspNetCore.Routing;

namespace Hive.MicroServices;

/// <summary>
/// Extension methods for registering auxiliary HTTP endpoints on an <see cref="IMicroService"/>.
/// </summary>
public static class IMicroServiceMapEndpointsExtensions
{
  /// <summary>
  /// Registers auxiliary HTTP routes that are served inside the selected pipeline
  /// mode's routing/CORS/authorization envelope, alongside the mode's own endpoints.
  /// Mode-agnostic and additive; does not affect <see cref="MicroServicePipelineMode"/>.
  /// </summary>
  /// <param name="microservice">The microservice to configure</param>
  /// <param name="map">A delegate that maps routes on the <see cref="IEndpointRouteBuilder"/></param>
  /// <returns><see cref="IMicroService"/></returns>
  /// <exception cref="ArgumentNullException">Thrown when any of the provided arguments are null</exception>
  public static IMicroService MapEndpoints(
    this IMicroService microservice,
    Action<IEndpointRouteBuilder> map)
  {
    _ = microservice ?? throw new ArgumentNullException(nameof(microservice));
    _ = map ?? throw new ArgumentNullException(nameof(map));

    var service = (MicroService)microservice;
    service.MapEndpointActions.Add(map);

    return microservice;
  }
}