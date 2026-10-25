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
  /// Default service pipeline (ConfigureDefaultServicePipeline): no application request-processing mode; serves probes and optional MapEndpoints routes
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
  Grpc,

  /// <summary>
  /// Model Context Protocol (MCP) server(s)
  /// </summary>
  Mcp,

  /// <summary>
  /// Background job / worker services (ConfigureJob). Execute IHostedService(s); custom HTTP routes via MapEndpoints are forbidden
  /// </summary>
  Job
}