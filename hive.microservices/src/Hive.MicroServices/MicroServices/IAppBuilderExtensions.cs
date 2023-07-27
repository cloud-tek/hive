using Microsoft.AspNetCore.Builder;

namespace Hive.MicroServices;

public static class IAppBuilderExtensions
{
  public static IApplicationBuilder When(this IApplicationBuilder app, Func<bool> predicate,
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
