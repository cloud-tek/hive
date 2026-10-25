using Hive.Exceptions;

namespace Hive.MicroServices.Extensions;

/// <summary>
/// Single home of the rule that Job (worker) services cannot expose custom HTTP routes.
/// </summary>
internal static class CustomEndpointsGuard
{
  /// <summary>
  /// Throws when custom endpoints were registered on a service in <see cref="MicroServicePipelineMode.Job"/> mode.
  /// </summary>
  /// <param name="service">The microservice to check</param>
  /// <exception cref="ConfigurationException">Thrown when the service is a Job and <c>MapEndpoints</c> was called</exception>
  /// <remarks>
  /// The check is deliberately keyed on <see cref="MicroServicePipelineMode.Job"/>, never on
  /// <see cref="MicroServicePipelineMode.None"/>: the Default pipeline also uses <c>None</c> and serves
  /// custom routes (ADR custom_endpoints_seam_design.md section 4.3 / 10).
  /// </remarks>
  internal static void ThrowIfCustomEndpointsForbidden(this MicroService service)
  {
    if (service.PipelineMode == MicroServicePipelineMode.Job && service.MapEndpointActions.Count > 0)
    {
      throw new ConfigurationException(Constants.Errors.MapEndpointsJobForbidden);
    }
  }
}