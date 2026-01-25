using Microsoft.Extensions.Configuration;

namespace Hive;

/// <summary>
/// ASP.NET Core-based microservice interface with Kubernetes probe support and pipeline configuration.
/// Extends IMicroServiceCore with ASP.NET-specific lifecycle management and hosting capabilities.
/// </summary>
/// <remarks>
/// This interface is specifically designed for ASP.NET Core microservices running in Kubernetes or
/// similar container orchestration environments. It includes:
/// - Kubernetes readiness and startup probes (IsReady, IsStarted)
/// - ASP.NET Core pipeline mode configuration (Api, GraphQL, gRPC, Job, etc.)
/// - Lifecycle event notifications (Lifetime)
/// - Traditional application hosting with exit codes (RunAsync)
///
/// For non-ASP.NET hosting models (e.g., Azure Functions), see IMicroServiceCore or specific
/// host interfaces like IFunctionHost.
/// </remarks>
public interface IMicroService : IMicroServiceCore
{
  /// <summary>
  /// Flag indicating if the microservice is ready to receive traffic. Used for Kubernetes readiness probe(s).
  /// </summary>
  bool IsReady { get; }

  /// <summary>
  /// Flag indicating if the microservice has completed its startup cycle. Used for Kubernetes startup probe(s).
  /// </summary>
  bool IsStarted { get; }

  /// <summary>
  /// The microservice's lifetime configuration for monitoring startup completion and failures
  /// </summary>
  IMicroServiceLifetime Lifetime { get; }

  /// <summary>
  /// The pre-configured pipeline mode for the microservice (Api, GraphQL, gRPC, Job, None)
  /// </summary>
  MicroServicePipelineMode PipelineMode { get; }

  /// <summary>
  /// Method to register additional MicroServiceExtensions with covariant return type.
  /// Extension must implement IMicroServiceExtension to ensure proper instantiation.
  /// </summary>
  /// <typeparam name="TExtension">
  /// Type of the MicroServiceExtension.
  /// Must implement IMicroServiceExtension&lt;TExtension&gt; with a Create factory method.
  /// </typeparam>
  /// <returns>The IMicroService instance for fluent chaining</returns>
  new IMicroService RegisterExtension<TExtension>()
    where TExtension : MicroServiceExtension<TExtension>, IMicroServiceExtension<TExtension>;

  /// <summary>
  /// Method to asynchronously initialize and start the microservice.
  /// Blocks until the microservice stops.
  /// </summary>
  /// <param name="configuration">Optional configuration root</param>
  /// <param name="args">Optional command line arguments</param>
  /// <returns>Exit code (0 for success, non-zero for failure)</returns>
  Task<int> RunAsync(IConfigurationRoot configuration = default!, params string[] args);

  /// <summary>
  /// Method to asynchronously initialize the microservice without starting it.
  /// </summary>
  /// <param name="configuration"></param>
  /// <param name="args"></param>
  /// <returns>Task</returns>
  Task InitializeAsync(IConfigurationRoot? configuration = null, params string[] args);

  /// <summary>
  /// Method to asynchronously start the microservice after initialization.
  /// Returns immediately after starting (does not block like RunAsync).
  /// </summary>
  /// <returns>Task that completes when the host has started</returns>
  Task StartAsync();

  /// <summary>
  /// Method to asynchronously stop the microservice.
  /// </summary>
  /// <returns>Task that completes when the host has stopped</returns>
  Task StopAsync();
}