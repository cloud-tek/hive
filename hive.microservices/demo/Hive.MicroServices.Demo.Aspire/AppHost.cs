var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Hive_MicroServices_Demo_Api>("hive-microservices-demo-api");

/*
 var apiService = builder.AddProject<Projects.PoC_ApiService>("apiservice")
       .WithHttpHealthCheck("/health");

   builder.AddProject<Projects.PoC_Web>("webfrontend")
       .WithExternalHttpEndpoints()
       .WithHttpHealthCheck("/health")
       .WithReference(apiService)
       .WaitFor(apiService);
 */

builder.Build().Run();