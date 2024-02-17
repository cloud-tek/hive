using Microsoft.AspNetCore.Builder;

namespace Hive;

/// <summary>
/// Interface for extensions that have request logging middleware
/// </summary>
public interface IHaveRequestLoggingMiddleware
{
  /// <summary>
  /// The action to configure the request logging middleware
  /// </summary>
  Action<IApplicationBuilder> ConfigureRequestLoggingMiddleware { get; }
}