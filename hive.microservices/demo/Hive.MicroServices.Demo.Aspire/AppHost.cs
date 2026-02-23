using Hive.MicroServices.Demo.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

// VictoriaMetrics Stack
_ = builder.AddContainer("victoria-metrics", "victoriametrics/victoria-metrics")
  .WithHttpEndpoint(port: 8428, targetPort: 8428, name: "http")
  .WithArgs("--storageDataPath=/victoria-metrics-data", "--httpListenAddr=:8428");

_ = builder.AddContainer("victoria-logs", "victoriametrics/victoria-logs")
  .WithHttpEndpoint(port: 9428, targetPort: 9428, name: "http")
  .WithArgs("--storageDataPath=/victoria-logs-data", "--httpListenAddr=:9428");

_ = builder.AddContainer("victoria-traces", "victoriametrics/victoria-traces")
  .WithHttpEndpoint(port: 10428, targetPort: 10428, name: "http")
  .WithArgs("--storageDataPath=/victoria-traces-data");

// Resolve Aspire dashboard OTLP endpoint for collector forwarding
var dashboardOtlpUrl = builder.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"]
  ?? builder.Configuration["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"]
  ?? "http://localhost:18889";

var dashboardEndpointForContainer = dashboardOtlpUrl
  .Replace("localhost", "host.docker.internal")
  .Replace("127.0.0.1", "host.docker.internal")
  .Replace("[::1]", "host.docker.internal");

// OpenTelemetry Collector — fans out to Aspire dashboard + VictoriaMetrics stack
var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib")
  .WithBindMount("otel-collector-config.yaml", "/config/otel-collector-config.yaml")
  .WithArgs("--config=/config/otel-collector-config.yaml")
  .WithEndpoint(targetPort: 4317, name: "grpc", scheme: "http")
  .WithEndpoint(targetPort: 4318, name: "http", scheme: "http")
  .WithEnvironment("ASPIRE_ENDPOINT", dashboardEndpointForContainer)
  .WithEnvironment("ASPIRE_API_KEY", builder.Configuration["AppHost:OtlpApiKey"] ?? string.Empty);

// Default Service
builder.AddProject<Projects.Hive_MicroServices_Demo>("hive-microservices-demo")
  .WithHttpHealthCheck("/status/readiness")
  .WithOtelCollector(otelCollector);

// HTTP Services
var apiControllers = builder.AddProject<Projects.Hive_MicroServices_Demo_ApiControllers>("hive-microservices-demo-apicontrollers")
  .WithHttpHealthCheck("/status/readiness")
  .WithOtelCollector(otelCollector);

builder.AddProject<Projects.Hive_MicroServices_Demo_Api>("hive-microservices-demo-api")
  .WithHttpHealthCheck("/status/readiness")
  .WithOtelCollector(otelCollector)
  .WithReference(apiControllers)
  .WithEnvironment("Hive__Http__IWeatherForecastApi__BaseAddress",
    () => $"http://{apiControllers.Resource.Name}");

builder.AddProject<Projects.Hive_MicroServices_Demo_GraphQL>("hive-microservices-demo-graphql")
  .WithHttpHealthCheck("/status/readiness")
  .WithOtelCollector(otelCollector);

// gRPC Services
builder.AddProject<Projects.Hive_MicroServices_Demo_Grpc>("hive-microservices-demo-grpc")
  .WithHttpHealthCheck("/status/readiness")
  .WithOtelCollector(otelCollector);

builder.AddProject<Projects.Hive_MicroServices_Demo_GrpcCodeFirst>("hive-microservices-demo-grpccodefirst")
  .WithHttpHealthCheck("/status/readiness")
  .WithOtelCollector(otelCollector);

// Background Services
builder.AddProject<Projects.Hive_MicroServices_Demo_Job>("hive-microservices-demo-job")
  .WithHttpHealthCheck("/status/readiness")
  .WithOtelCollector(otelCollector);

builder.Build().Run();