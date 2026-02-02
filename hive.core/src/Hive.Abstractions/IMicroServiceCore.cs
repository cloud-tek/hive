using Microsoft.Extensions.Configuration;

namespace Hive;

/// <summary>
/// Core abstraction for all Hive service hosts (microservices, Azure Functions, etc.)
/// Provides fundamental lifecycle management and configuration capabilities without
/// tying to any specific hosting model (ASP.NET Core, Azure Functions, etc.)
/// </summary>
/// <remarks>
/// This interface represents the framework-agnostic foundation for all Hive hosts.
/// It includes identity, configuration, extension system, and lifecycle management
/// but deliberately excludes hosting-model-specific concerns like:
/// - Kubernetes probe support (IsReady, IsStarted) - see IMicroService
/// - ASP.NET Core pipeline modes - see IMicroService
/// - Platform-specific execution patterns - see concrete implementations
/// </remarks>
public interface IMicroServiceCore : IAsyncDisposable, IDisposable
{
  /// <summary>
  /// The name of the service
  /// </summary>
  string Name { get; }

  /// <summary>
  /// The unique identifier of the service instance
  /// </summary>
  string Id { get; }

  /// <summary>
  /// The environment for the service (Development, Staging, Production, etc.)
  /// </summary>
  string Environment { get; }

  /// <summary>
  /// The configuration root for the service
  /// </summary>
  IConfigurationRoot ConfigurationRoot { get; }

  /// <summary>
  /// The environment variables for the service
  /// </summary>
  IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

  /// <summary>
  /// The command line arguments for the service
  /// </summary>
  string[] Args { get; }

  /// <summary>
  /// Flag indicating if an externally provided logger is used
  /// </summary>
  bool ExternalLogger { get; }

  /// <summary>
  /// Enum indicating the hosting mode of the service (Process, Container, etc.)
  /// </summary>
  MicroServiceHostingMode HostingMode { get; }

  /// <summary>
  /// The extensions registered with this service
  /// </summary>
  IList<MicroServiceExtension> Extensions { get; }

  /// <summary>
  /// The cancellation token source for the service
  /// </summary>
  CancellationTokenSource CancellationTokenSource { get; }

  /// <summary>
  /// Method to register additional MicroServiceExtensions.
  /// Extension must implement IMicroServiceExtension to ensure proper instantiation.
  /// </summary>
  /// <typeparam name="TExtension">
  /// Type of the MicroServiceExtension.
  /// Must implement IMicroServiceExtension&lt;TExtension&gt; with a Create factory method.
  /// </typeparam>
  /// <returns>The IMicroServiceCore instance for fluent chaining</returns>
  IMicroServiceCore RegisterExtension<TExtension>()
    where TExtension : MicroServiceExtension<TExtension>, IMicroServiceExtension<TExtension>;

  /// <summary>
  /// Method to asynchronously initialize the service without starting it.
  /// </summary>
  /// <param name="configuration">Optional configuration root</param>
  /// <param name="args">Optional command line arguments</param>
  /// <returns>Task that completes when initialization is done</returns>
  Task InitializeAsync(IConfigurationRoot? configuration = null, params string[] args);

  /// <summary>
  /// Method to asynchronously start the service after initialization.
  /// Returns immediately after starting (does not block).
  /// </summary>
  /// <returns>Task that completes when the host has started</returns>
  Task StartAsync();

  /// <summary>
  /// Method to asynchronously stop the service.
  /// </summary>
  /// <returns>Task that completes when the host has stopped</returns>
  Task StopAsync();
}