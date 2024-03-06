using Microsoft.Extensions.DependencyInjection;

namespace Hive.MicroServices.Lifecycle;

/// <summary>
/// Extensions for adding <see cref="IHostedStartupService"/> to <see cref="IServiceCollection"/>
/// </summary>
public static class HostedStartupServiceExtensions
{
  /// <summary>
  /// Adds a <see cref="IHostedStartupService"/> to the <see cref="IServiceCollection"/>
  /// </summary>
  /// <typeparam name="T">Type of the <see cref="IHostedStartupService"/></typeparam>
  /// <param name="services"></param>
  /// <returns><see cref="IServiceCollection"/></returns>
  public static IServiceCollection AddHostedStartupService<T>(this IServiceCollection services)
      where T : class, IHostedStartupService
  {
    return services.AddSingleton<IHostedStartupService, T>();
  }
}