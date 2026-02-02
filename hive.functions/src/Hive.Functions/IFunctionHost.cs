using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.Functions;

/// <summary>
/// Represents a function host that provides Azure Functions integration with the Hive framework
/// </summary>
public interface IFunctionHost : IMicroServiceCore
{
  /// <summary>
  /// Configures services for dependency injection
  /// </summary>
  /// <param name="configure">Configuration action</param>
  /// <returns>The function host instance for fluent chaining</returns>
  IFunctionHost ConfigureServices(Action<IServiceCollection, IConfiguration> configure);

  /// <summary>
  /// Configures Azure Functions-specific settings
  /// </summary>
  /// <param name="configure">Configuration action</param>
  /// <returns>The function host instance for fluent chaining</returns>
  IFunctionHost ConfigureFunctions(Action<IFunctionsWorkerApplicationBuilder> configure);

  /// <summary>
  /// Runs the function host
  /// </summary>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>A task representing the asynchronous operation</returns>
  Task RunAsync(CancellationToken cancellationToken = default);
}