using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Hive.MicroServices.Extensions;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/>
/// </summary>
public static class IAppBuilderExtensions
{
  /// <summary>
  /// Drains all custom endpoint mapping actions registered via <c>MapEndpoints</c> onto the given <see cref="IEndpointRouteBuilder"/>.
  /// </summary>
  /// <param name="endpoints">The endpoint route builder</param>
  /// <param name="service">The microservice whose <c>MapEndpointActions</c> are drained</param>
  internal static void DrainCustomEndpoints(this IEndpointRouteBuilder endpoints, MicroService service)
  {
    foreach (var map in service.MapEndpointActions)
    {
      map(endpoints);
    }
  }

  /// <summary>
  /// Executes the action if the predicate is true
  /// </summary>
  /// <param name="app"></param>
  /// <param name="predicate"></param>
  /// <param name="action"></param>
  /// <returns><see cref="IApplicationBuilder"/></returns>
  /// <exception cref="ArgumentNullException">thrown when any of the provided arguments are null</exception>
  public static IApplicationBuilder When(
    this IApplicationBuilder app,
    Func<bool> predicate,
    Action<IApplicationBuilder> action)
  {
    _ = app ?? throw new ArgumentNullException(nameof(app));
    _ = predicate ?? throw new ArgumentNullException(nameof(predicate));
    _ = action ?? throw new ArgumentNullException(nameof(action));

    if (predicate())
    {
      action(app);
    }

    return app;
  }
}