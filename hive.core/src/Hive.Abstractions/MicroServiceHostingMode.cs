namespace Hive;

/// <summary>
/// The hosting mode of the microservice
/// </summary>
public enum MicroServiceHostingMode
{
  /// <summary>
  /// The microservice is being hosted in-process
  /// </summary>
  Process,

  /// <summary>
  /// The microservice is being hosted in a container
  /// </summary>
  Container,

  /// <summary>
  /// The microservice is being hosted in Kubernetes
  /// </summary>
  Kubernetes
}