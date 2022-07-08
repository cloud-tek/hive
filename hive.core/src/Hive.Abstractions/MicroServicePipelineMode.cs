namespace Hive;

public enum MicroServicePipelineMode
{
    NotSet,             // Default value. Service must not start without specifying the pipeline mode
    None,               // Fire & forget jobs. Services which do not use a request processing pipeline
    ApiControllers,     // REST'ful API(s) (controllers)
    Api,                // REST'ful API(s) (minimalistic)
    GraphQL,            // GraphQL API(s)
    Grpc                // GRPC API(s)
}
