namespace Hive;

/// <summary>
/// The pipeline mode of the microservice
/// </summary>
public enum MicroServicePipelineMode
{
  /// <summary>
  /// Default value. Service must not start without specifying the pipeline mode
  /// </summary>
  NotSet,

  /// <summary>
  /// Fire and forget jobs. Services which do not use a request processing pipeline
  /// </summary>
  None,

  /// <summary>
  /// REST'ful API(s) (controllers)
  /// </summary>
  ApiControllers,

  /// <summary>
  /// REST'ful API(s) (minimalistic)
  /// </summary>
  Api,

  /// <summary>
  /// GraphQL API(s)
  /// </summary>
  GraphQL,

  /// <summary>
  /// GRPC API(s)
  /// </summary>
  Grpc
}