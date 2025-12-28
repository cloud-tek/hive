using Microsoft.Extensions.Configuration;

namespace Hive;

/// <summary>
/// The base IMicroService interface. All Hive microservices implement this interface.
/// </summary>
public interface IMicroService
{
  /// <summary>
  /// The cancellation token source for the microservice
  /// </summary>
  CancellationTokenSource CancellationTokenSource { get; }

  /// <summary>
  /// The configuration root for the microservice
  /// </summary>
  IConfigurationRoot ConfigurationRoot { get; }

  /// <summary>
  /// The environment for the microservice
  /// </summary>
  string Environment { get; }

  /// <summary>
  /// The command line arguments for the microservice
  /// </summary>
  string[] Args { get; }

  /// <summary>
  /// The environment variables for the microservice
  /// </summary>
  IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

  /// <summary>
  /// The extensions for the microservice
  /// </summary>
  IList<MicroServiceExtension> Extensions { get; }

  /// <summary>
  /// Flag indicating if an externally provided logger is used
  /// </summary>
  bool ExternalLogger { get; }

  /// <summary>
  /// Enum indicating the hosting mode of the microservice
  /// </summary>
  MicroServiceHostingMode HostingMode { get; }

  /// <summary>
  /// The Id of the microservice instance
  /// </summary>
  string Id { get; }

  /// <summary>
  /// Flag indicating if the microservice is ready to receive traffic. Used for k8s readiness probe(s)
  /// </summary>
  bool IsReady { get; }

  /// <summary>
  /// Flag indicating if the microservice has completed it's startup cycle. Used for k8s startup probe(s)
  /// </summary>
  bool IsStarted { get; }

  /// <summary>
  /// The microservice's lifetime configuration
  /// </summary>
  IMicroServiceLifetime Lifetime { get; }

  /// <summary>
  /// The name of the microsevice
  /// </summary>
  string Name { get; }

  /// <summary>
  /// The pre-configured pipeline mode for the microservice
  /// </summary>
  MicroServicePipelineMode PipelineMode { get; }

  /// <summary>
  /// Method to register additional MicroServiceExtensions
  /// </summary>
  /// <typeparam name="TExtension">Type of the MicroServiceExtension</typeparam>
  /// <returns>The IMicroService instance</returns>
  IMicroService RegisterExtension<TExtension>() where TExtension : MicroServiceExtension, new();

  /// <summary>
  /// Method to asynchonoously initialize and start the microservice.
  /// Blocks until the microservice stops.
  /// </summary>
  /// <param name="configuration"></param>
  /// <param name="args"></param>
  /// <returns>Exit code</returns>
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