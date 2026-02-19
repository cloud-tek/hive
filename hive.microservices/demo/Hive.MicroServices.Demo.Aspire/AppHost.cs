var builder = DistributedApplication.CreateBuilder(args);

// Default Service
builder.AddProject<Projects.Hive_MicroServices_Demo>("hive-microservices-demo")
  .WithHttpHealthCheck("/status/readiness");

// HTTP Services
builder.AddProject<Projects.Hive_MicroServices_Demo_Api>("hive-microservices-demo-api")
  .WithHttpHealthCheck("/status/readiness");

builder.AddProject<Projects.Hive_MicroServices_Demo_ApiControllers>("hive-microservices-demo-apicontrollers")
  .WithHttpHealthCheck("/status/readiness");

builder.AddProject<Projects.Hive_MicroServices_Demo_GraphQL>("hive-microservices-demo-graphql")
  .WithHttpHealthCheck("/status/readiness");

// gRPC Services
builder.AddProject<Projects.Hive_MicroServices_Demo_Grpc>("hive-microservices-demo-grpc")
  .WithHttpHealthCheck("/status/readiness");

builder.AddProject<Projects.Hive_MicroServices_Demo_GrpcCodeFirst>("hive-microservices-demo-grpccodefirst")
  .WithHttpHealthCheck("/status/readiness");

// Background Services
builder.AddProject<Projects.Hive_MicroServices_Demo_Job>("hive-microservices-demo-job")
  .WithHttpHealthCheck("/status/readiness");

builder.Build().Run();