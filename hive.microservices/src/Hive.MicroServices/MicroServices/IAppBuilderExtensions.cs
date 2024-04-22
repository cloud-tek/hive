using Microsoft.AspNetCore.Builder;

namespace Hive.MicroServices;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/>
/// </summary>
public static class IAppBuilderExtensions
{
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