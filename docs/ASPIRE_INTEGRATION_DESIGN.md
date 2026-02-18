# Aspire Integration Design

**Status**: Draft Design Document
**Date**: 2026-02-06
**Author**: Architecture Team

---

## Executive Summary

This document outlines the design for integrating .NET Aspire orchestration into the Hive microservices framework's demo applications. The integration will enable local development and testing of all demo services under a unified orchestration platform with built-in observability through OpenTelemetry and VictoriaMetrics stack integration.

### Key Design Principles

1. **Sequential Onboarding** - Incrementally add demo projects to Aspire orchestration
2. **OpenTelemetry First** - Ensure telemetry flows correctly before moving forward
3. **Production-like Observability** - Use VictoriaMetrics stack (Metrics, Logs, Traces) for realistic monitoring
4. **Minimal Service Changes** - Demos should work standalone or under Aspire orchestration
5. **Developer Experience** - Single command to start entire Hive ecosystem locally

---

## 1. Current State

### Existing Structure

```
hive.microservices/demo/
├── Hive.MicroServices.Demo.Api/                  ✅ Currently in Aspire
├── Hive.MicroServices.Demo.ApiControllers/       ⏳ To be onboarded
├── Hive.MicroServices.Demo.GraphQL/              ⏳ To be onboarded
├── Hive.MicroServices.Demo.Grpc/                 ⏳ To be onboarded
├── Hive.MicroServices.Demo.GrpcCodeFirst/        ⏳ To be onboarded
├── Hive.MicroServices.Demo.Job/                  ⏳ To be onboarded
├── Hive.MicroServices.Demo.Aspire/               ✅ Orchestrator (AppHost)
└── Hive.MicroServices.Demo.ServiceDefaults/      ✅ Shared defaults
```

### Current Aspire Configuration

The `Hive.MicroServices.Demo.Aspire` project currently orchestrates only the Demo.Api service:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Hive_MicroServices_Demo_Api>("hive-microservices-demo-api");

builder.Build().Run();
```

---

## 2. Goals and Objectives

### Primary Goals

1. **Full Demo Coverage** - All demo projects running under Aspire orchestration
2. **Telemetry Validation** - Verify OpenTelemetry instrumentation works end-to-end
3. **Observability Stack** - Integrate VictoriaMetrics, VictoriaLogs, and VictoriaTraces
4. **Health Monitoring** - Leverage built-in health checks and readiness probes
5. **Service Discovery** - Enable inter-service communication via Aspire service discovery

### Success Criteria

- [ ] All 6 demo services successfully start under Aspire
- [ ] OpenTelemetry traces flow from services → VictoriaTraces
- [ ] OpenTelemetry logs flow from services → VictoriaLogs
- [ ] OpenTelemetry metrics flow from services → VictoriaMetrics
- [ ] Aspire dashboard shows all services as healthy
- [ ] Service-to-service communication works (e.g., Api → gRPC)
- [ ] Single `dotnet run` command starts entire stack

---

## 3. Architecture

### 3.1 Aspire Orchestration Model

```
┌─────────────────────────────────────────────────────────────┐
│  Hive.MicroServices.Demo.Aspire (AppHost)                   │
│  - Service orchestration                                     │
│  - Environment configuration                                 │
│  - Service dependencies                                      │
│  - Health check aggregation                                  │
└─────────────────────────────────────────────────────────────┘
                          │
         ┌────────────────┼────────────────┐
         ▼                ▼                ▼
    ┌─────────┐     ┌─────────┐     ┌─────────┐
    │   Api   │     │ GraphQL │     │  gRPC   │
    └─────────┘     └─────────┘     └─────────┘
         │                │                │
         └────────────────┴────────────────┘
                          │
                          ▼
              ┌───────────────────────┐
              │ OpenTelemetry (OTLP)  │
              └───────────────────────┘
                          │
         ┌────────────────┼────────────────┐
         ▼                ▼                ▼
    ┌─────────┐     ┌─────────┐     ┌─────────┐
    │Victoria │     │Victoria │     │Victoria │
    │ Metrics │     │  Logs   │     │ Traces  │
    └─────────┘     └─────────┘     └─────────┘
```

### 3.2 Service Defaults Pattern

Aspire uses a "ServiceDefaults" project to share common configuration across all services:

**Purpose of ServiceDefaults:**
- OpenTelemetry configuration (OTLP exporters)
- Health check defaults
- Resilience patterns (retry, circuit breaker)
- Service discovery configuration
- Logging configuration

**Implementation:**
```csharp
// In ServiceDefaults project
public static class ServiceDefaultsExtensions
{
    public static IHostApplicationBuilder AddServiceDefaults(
        this IHostApplicationBuilder builder)
    {
        // OpenTelemetry configuration
        builder.AddOpenTelemetryExporters();

        // Health checks
        builder.Services.AddDefaultHealthChecks();

        // Service discovery
        builder.Services.AddServiceDiscovery();

        return builder;
    }
}
```

### 3.3 VictoriaMetrics Stack Integration

**VictoriaMetrics Components:**

1. **VictoriaMetrics** - Time-series database for metrics (Prometheus-compatible)
2. **VictoriaLogs** - Log aggregation and search
3. **VictoriaTraces** - Distributed tracing backend (Jaeger-compatible)

**Integration Approach:**

```csharp
// In AppHost (Aspire orchestrator)
var victoriametrics = builder.AddContainer("victoriametrics", "victoriametrics/victoria-metrics")
    .WithBindMount("./victoria-data", "/victoria-metrics-data")
    .WithHttpEndpoint(port: 8428, targetPort: 8428, name: "http");

var victorialogs = builder.AddContainer("victorialogs", "victoriametrics/victoria-logs")
    .WithBindMount("./victoria-logs-data", "/victoria-logs-data")
    .WithHttpEndpoint(port: 9428, targetPort: 9428, name: "http");

var victoriatraces = builder.AddContainer("victoriatraces", "victoriametrics/victoria-traces")
    .WithHttpEndpoint(port: 14268, targetPort: 14268, name: "jaeger");

// Configure OTLP endpoints to point to VictoriaMetrics stack
builder.AddProject<Projects.Hive_MicroServices_Demo_Api>("demo-api")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
    .WaitFor(victoriametrics);
```

---

## 4. Sequential Onboarding Plan

### Phase 1: Foundation (Current State) ✅

**Scope:**
- ✅ Aspire AppHost project created
- ✅ ServiceDefaults project created
- ✅ Demo.Api integrated

**Status:** Complete

### Phase 2: HTTP Services

**Scope:**
- Onboard `Hive.MicroServices.Demo.ApiControllers`
- Onboard `Hive.MicroServices.Demo.GraphQL`
- Verify HTTP health checks
- Test OpenTelemetry HTTP instrumentation

**AppHost Changes:**
```csharp
var apiControllers = builder.AddProject<Projects.Hive_MicroServices_Demo_ApiControllers>("demo-api-controllers")
    .WithHttpHealthCheck("/status/readiness");

var graphql = builder.AddProject<Projects.Hive_MicroServices_Demo_GraphQL>("demo-graphql")
    .WithHttpHealthCheck("/status/readiness");
```

**Validation:**
- [ ] Services appear in Aspire dashboard
- [ ] Health checks pass
- [ ] HTTP traces visible in telemetry

### Phase 3: gRPC Services

**Scope:**
- Onboard `Hive.MicroServices.Demo.Grpc`
- Onboard `Hive.MicroServices.Demo.GrpcCodeFirst`
- Configure gRPC health checks
- Test gRPC instrumentation

**AppHost Changes:**
```csharp
var grpc = builder.AddProject<Projects.Hive_MicroServices_Demo_Grpc>("demo-grpc")
    .WithHttpHealthCheck("/status/readiness");

var grpcCodeFirst = builder.AddProject<Projects.Hive_MicroServices_Demo_GrpcCodeFirst>("demo-grpc-codefirst")
    .WithHttpHealthCheck("/status/readiness");
```

**Validation:**
- [ ] gRPC services accessible
- [ ] gRPC traces visible
- [ ] gRPC health checks working

### Phase 4: Background Services

**Scope:**
- Onboard `Hive.MicroServices.Demo.Job`
- Configure background service health checks
- Validate job execution telemetry

**AppHost Changes:**
```csharp
var job = builder.AddProject<Projects.Hive_MicroServices_Demo_Job>("demo-job")
    .WithHttpHealthCheck("/status/readiness");
```

**Validation:**
- [ ] Job service starts successfully
- [ ] Background work executes
- [ ] Telemetry captured for job execution

### Phase 5: VictoriaMetrics Integration

**Scope:**
- Add VictoriaMetrics container
- Add VictoriaLogs container
- Add VictoriaTraces container
- Configure OTLP exporters to target VictoriaMetrics stack
- Set up Grafana dashboards (optional)

**AppHost Changes:**
```csharp
// Add VictoriaMetrics stack
var victoriametrics = builder.AddContainer("victoriametrics", "victoriametrics/victoria-metrics")
    .WithBindMount("./victoria-data", "/victoria-metrics-data")
    .WithHttpEndpoint(port: 8428, targetPort: 8428, name: "http")
    .WithArgs("-storageDataPath=/victoria-metrics-data", "-retentionPeriod=7d");

var victorialogs = builder.AddContainer("victorialogs", "victoriametrics/victoria-logs")
    .WithBindMount("./victoria-logs-data", "/victoria-logs-data")
    .WithHttpEndpoint(port: 9428, targetPort: 9428, name: "http");

// VictoriaTraces is Jaeger-compatible
var victoriatraces = builder.AddContainer("victoriatraces", "victoriametrics/victoria-traces")
    .WithHttpEndpoint(port: 14268, targetPort: 14268, name: "jaeger")
    .WithHttpEndpoint(port: 16686, targetPort: 16686, name: "ui");

// OTLP Collector to forward to VictoriaMetrics
var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib")
    .WithBindMount("./otel-collector-config.yaml", "/etc/otelcol/config.yaml")
    .WithHttpEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")
    .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
    .WaitFor(victoriametrics)
    .WaitFor(victorialogs)
    .WaitFor(victoriatraces);

// Update all services to use OTLP collector
foreach (var project in new[] { api, apiControllers, graphql, grpc, grpcCodeFirst, job })
{
    project.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317");
}
```

**OTLP Collector Configuration** (`otel-collector-config.yaml`):
```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

processors:
  batch:

exporters:
  # Metrics to VictoriaMetrics
  prometheusremotewrite:
    endpoint: http://victoriametrics:8428/api/v1/write

  # Logs to VictoriaLogs
  loki:
    endpoint: http://victorialogs:9428/loki/api/v1/push

  # Traces to VictoriaTraces (Jaeger protocol)
  jaeger:
    endpoint: victoriatraces:14250
    tls:
      insecure: true

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [jaeger]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheusremotewrite]
    logs:
      receivers: [otlp]
      processors: [batch]
      exporters: [loki]
```

**Validation:**
- [ ] VictoriaMetrics UI accessible at http://localhost:8428
- [ ] VictoriaLogs UI accessible at http://localhost:9428
- [ ] VictoriaTraces UI accessible at http://localhost:16686
- [ ] Metrics queryable in VictoriaMetrics
- [ ] Logs searchable in VictoriaLogs
- [ ] Traces visible in VictoriaTraces UI

### Phase 6: Inter-Service Communication

**Scope:**
- Demonstrate Api → gRPC communication
- Demonstrate Api → GraphQL communication
- Use Aspire service discovery
- Validate distributed tracing across services

**Implementation Example:**
```csharp
// In Demo.Api, call Demo.Grpc service
var grpcClient = builder.Services.AddGrpcClient<GreeterClient>(options =>
{
    // Aspire service discovery resolves "demo-grpc"
    options.Address = new Uri("http://demo-grpc");
});

// AppHost service references
builder.AddProject<Projects.Hive_MicroServices_Demo_Api>("demo-api")
    .WithReference(grpc)        // Creates service discovery entry
    .WithReference(graphql);    // Creates service discovery entry
```

**Validation:**
- [ ] Service discovery resolves service names
- [ ] Cross-service calls succeed
- [ ] Distributed traces show full request chain
- [ ] Context propagation works (trace IDs, baggage)

---

## 5. OpenTelemetry Configuration

### 5.1 Current Hive OpenTelemetry Integration

Hive already has `Hive.OpenTelemetry` extension:

```csharp
new MicroService("service-name")
    .WithOpenTelemetry(
        logging: builder => { /* customize */ },
        tracing: builder => { /* customize */ },
        metrics: builder => { /* customize */ }
    );
```

### 5.2 Aspire ServiceDefaults Integration

**Strategy:** Augment Hive's OpenTelemetry with Aspire defaults

```csharp
// In ServiceDefaults
public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
{
    // Let Hive configure OpenTelemetry base
    // Then add Aspire-specific enrichment

    builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddGrpcClientInstrumentation();
    });

    builder.Services.ConfigureOpenTelemetryMeterProvider(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        metrics.AddRuntimeInstrumentation();
    });

    return builder;
}
```

### 5.3 Environment Variables

**Set by Aspire automatically:**
- `OTEL_SERVICE_NAME` - Set to Aspire resource name
- `OTEL_RESOURCE_ATTRIBUTES` - Includes service.instance.id, deployment.environment

**Required for VictoriaMetrics:**
- `OTEL_EXPORTER_OTLP_ENDPOINT` - OTLP collector endpoint (http://localhost:4317)
- `OTEL_EXPORTER_OTLP_PROTOCOL` - Protocol: grpc or http/protobuf

---

## 6. Health Checks Strategy

### 6.1 Hive Built-in Probes

Hive microservices already expose:
- `/startup` - Kubernetes startup probe
- `/readiness` - Kubernetes readiness probe
- `/liveness` - Kubernetes liveness probe

### 6.2 Aspire Health Check Integration

```csharp
// In AppHost
builder.AddProject<Projects.Hive_MicroServices_Demo_Api>("demo-api")
    .WithHttpHealthCheck("/status/readiness");  // Uses Hive's readiness endpoint
```

> **Note:** `/status/startup` is a Kubernetes startup probe (one-shot, 200 once started) and is not
> suitable for Aspire's continuous health polling. Only `/status/readiness` should be used with
> `.WithHttpHealthCheck()` as it reflects ongoing service health.

**Health Check Flow:**
1. Service starts → Aspire detects process/port availability
2. Once started → `/status/readiness` polled continuously
3. Aspire dashboard reflects health status
4. Unhealthy services shown in red

---

## 7. Demo Projects Configuration

### 7.1 Minimal Service Changes Required

Each demo should:

1. **Reference ServiceDefaults** (if using Aspire-specific features)
2. **Keep existing Hive configuration** (WithOpenTelemetry, ConfigureApiPipeline, etc.)
3. **Work standalone** without Aspire (for unit testing, direct runs)

**Example - Demo.Api with Aspire:**

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Optional: Add Aspire defaults (service discovery, additional instrumentation)
#if ASPIRE
builder.AddServiceDefaults();
#endif

// Standard Hive configuration (works with or without Aspire)
var service = new MicroService("demo-api")
    .WithOpenTelemetry(
        logging: log => { },
        tracing: trace => { }
    )
    .ConfigureApiPipeline(app =>
    {
        app.MapGet("/hello", () => "Hello from Demo.Api");
    });

return await service.RunAsync(builder.Configuration, args);
```

### 7.2 Project References

**AppHost Project:**
```xml
<ItemGroup>
  <ProjectReference Include="..\Hive.MicroServices.Demo.Api\*.csproj" />
  <ProjectReference Include="..\Hive.MicroServices.Demo.ApiControllers\*.csproj" />
  <ProjectReference Include="..\Hive.MicroServices.Demo.GraphQL\*.csproj" />
  <ProjectReference Include="..\Hive.MicroServices.Demo.Grpc\*.csproj" />
  <ProjectReference Include="..\Hive.MicroServices.Demo.GrpcCodeFirst\*.csproj" />
  <ProjectReference Include="..\Hive.MicroServices.Demo.Job\*.csproj" />
</ItemGroup>
```

---

## 8. Testing Strategy

### 8.1 Local Development Workflow

**Start entire stack:**
```bash
cd hive.microservices/demo/Hive.MicroServices.Demo.Aspire
dotnet run
```

**Access Aspire Dashboard:**
- URL: https://localhost:17285 (or configured port)
- View: Service list, logs, traces, metrics

**Access VictoriaMetrics:**
- VictoriaMetrics: http://localhost:8428
- VictoriaLogs: http://localhost:9428
- VictoriaTraces: http://localhost:16686

### 8.2 Validation Tests

**Phase 2 - HTTP Services:**
```bash
# Test Api
curl http://localhost:5000/hello

# Test ApiControllers
curl http://localhost:5001/weatherforecast

# Test GraphQL
curl -X POST http://localhost:5002/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "{ hello }"}'
```

**Phase 3 - gRPC Services:**
```bash
# Test gRPC (using grpcurl)
grpcurl -plaintext localhost:5003 greet.Greeter/SayHello
```

**Phase 5 - VictoriaMetrics Validation:**
```bash
# Query metrics
curl 'http://localhost:8428/api/v1/query?query=up'

# Query logs (VictoriaLogs)
curl 'http://localhost:9428/select/logsql/query' \
  -d 'query=*' \
  -d 'limit=10'

# Traces available via UI: http://localhost:16686
```

### 8.3 Telemetry Validation Checklist

For each service:
- [ ] Traces appear in VictoriaTraces with correct service name
- [ ] Logs appear in VictoriaLogs with structured fields
- [ ] Metrics appear in VictoriaMetrics (http_server_request_duration, etc.)
- [ ] Resource attributes populated (service.name, service.instance.id)
- [ ] Context propagation works across service boundaries

---

## 9. Implementation Checklist

### Phase 2: HTTP Services
- [ ] Add ApiControllers project reference to AppHost.csproj
- [ ] Add GraphQL project reference to AppHost.csproj
- [ ] Configure ApiControllers in AppHost.cs with health checks
- [ ] Configure GraphQL in AppHost.cs with health checks
- [ ] Test services start successfully
- [ ] Validate HTTP traces in Aspire dashboard

### Phase 3: gRPC Services
- [ ] Add Grpc project reference to AppHost.csproj
- [ ] Add GrpcCodeFirst project reference to AppHost.csproj
- [ ] Configure Grpc in AppHost.cs with health checks
- [ ] Configure GrpcCodeFirst in AppHost.cs with health checks
- [ ] Test gRPC services accessible
- [ ] Validate gRPC traces

### Phase 4: Background Services
- [ ] Add Job project reference to AppHost.csproj
- [ ] Configure Job in AppHost.cs
- [ ] Validate background execution
- [ ] Check job telemetry

### Phase 5: VictoriaMetrics Stack
- [ ] Create otel-collector-config.yaml
- [ ] Add VictoriaMetrics container to AppHost
- [ ] Add VictoriaLogs container to AppHost
- [ ] Add VictoriaTraces container to AppHost
- [ ] Add OTLP Collector container to AppHost
- [ ] Configure OTEL_EXPORTER_OTLP_ENDPOINT for all services
- [ ] Create docker-compose.yml for VictoriaMetrics stack (alternative approach)
- [ ] Verify metrics in VictoriaMetrics UI
- [ ] Verify logs in VictoriaLogs UI
- [ ] Verify traces in VictoriaTraces UI

### Phase 6: Inter-Service Communication
- [ ] Add service references in AppHost (WithReference)
- [ ] Implement Api → gRPC call
- [ ] Implement Api → GraphQL call
- [ ] Validate distributed tracing
- [ ] Test service discovery

---

## 10. Future Enhancements

### 10.1 Grafana Integration

Add Grafana for unified dashboards:

```csharp
var grafana = builder.AddContainer("grafana", "grafana/grafana")
    .WithBindMount("./grafana/provisioning", "/etc/grafana/provisioning")
    .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "http")
    .WaitFor(victoriametrics)
    .WaitFor(victorialogs)
    .WaitFor(victoriatraces);
```

**Pre-configured Dashboards:**
- Hive service overview (requests, errors, latency)
- gRPC metrics
- Background job metrics
- Infrastructure metrics (CPU, memory, GC)

### 10.2 Persistent Storage

Currently using bind mounts for VictoriaMetrics data. Consider:
- Docker volumes for better portability
- Volume retention policies
- Backup/restore procedures

### 10.3 Load Testing Integration

Add k6 or JMeter containers for load testing:

```csharp
var k6 = builder.AddContainer("k6", "grafana/k6")
    .WithBindMount("./k6-scripts", "/scripts")
    .WithArgs("run", "/scripts/load-test.js")
    .WaitFor(api);
```

### 10.4 Database Services

When demos require persistence:

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var redis = builder.AddRedis("redis");

builder.AddProject<Projects.Hive_MicroServices_Demo_Api>("demo-api")
    .WithReference(postgres)
    .WithReference(redis);
```

---

## 11. Success Metrics

### Developer Experience
- Time to start full stack: < 60 seconds
- Time to see telemetry: < 10 seconds after service start
- Single command startup: `dotnet run` in Aspire project

### Observability
- 100% of HTTP requests traced
- 100% of gRPC calls traced
- Background job execution metrics captured
- Service health visible in real-time

### Reliability
- Services restart automatically on failure
- Health checks detect unhealthy services
- Graceful shutdown on Ctrl+C

---

## 12. References

### Documentation
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [VictoriaMetrics Documentation](https://docs.victoriametrics.com/)
- [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/)
- Hive OpenTelemetry: [Hive.OpenTelemetry/Extension.cs](../hive.opentelemetry/src/Hive.OpenTelemetry/Extension.cs)

### Related Design Documents
- [Hive Functions Design](HIVE_FUNCTIONS_DESIGN.md)
- [CORS Extraction Analysis](CORS_EXTRACTION_ANALYSIS.md)

---

## Appendix A: Complete AppHost.cs (Target State)

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// VictoriaMetrics Stack
var victoriametrics = builder.AddContainer("victoriametrics", "victoriametrics/victoria-metrics")
    .WithBindMount("./victoria-data", "/victoria-metrics-data")
    .WithHttpEndpoint(port: 8428, targetPort: 8428, name: "http")
    .WithArgs("-storageDataPath=/victoria-metrics-data", "-retentionPeriod=7d");

var victorialogs = builder.AddContainer("victorialogs", "victoriametrics/victoria-logs")
    .WithBindMount("./victoria-logs-data", "/victoria-logs-data")
    .WithHttpEndpoint(port: 9428, targetPort: 9428, name: "http");

var victoriatraces = builder.AddContainer("victoriatraces", "victoriametrics/victoria-traces")
    .WithHttpEndpoint(port: 14268, targetPort: 14268, name: "jaeger")
    .WithHttpEndpoint(port: 16686, targetPort: 16686, name: "ui");

// OTLP Collector
var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib")
    .WithBindMount("./otel-collector-config.yaml", "/etc/otelcol/config.yaml")
    .WithHttpEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")
    .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
    .WaitFor(victoriametrics)
    .WaitFor(victorialogs)
    .WaitFor(victoriatraces);

// Demo Services - HTTP
var api = builder.AddProject<Projects.Hive_MicroServices_Demo_Api>("demo-api")
    .WithHttpHealthCheck("/status/readiness")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
    .WaitFor(otelCollector);

var apiControllers = builder.AddProject<Projects.Hive_MicroServices_Demo_ApiControllers>("demo-api-controllers")
    .WithHttpHealthCheck("/status/readiness")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
    .WaitFor(otelCollector);

var graphql = builder.AddProject<Projects.Hive_MicroServices_Demo_GraphQL>("demo-graphql")
    .WithHttpHealthCheck("/status/readiness")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
    .WaitFor(otelCollector);

// Demo Services - gRPC
var grpc = builder.AddProject<Projects.Hive_MicroServices_Demo_Grpc>("demo-grpc")
    .WithHttpHealthCheck("/status/readiness")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
    .WaitFor(otelCollector);

var grpcCodeFirst = builder.AddProject<Projects.Hive_MicroServices_Demo_GrpcCodeFirst>("demo-grpc-codefirst")
    .WithHttpHealthCheck("/status/readiness")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
    .WaitFor(otelCollector);

// Demo Services - Background
var job = builder.AddProject<Projects.Hive_MicroServices_Demo_Job>("demo-job")
    .WithHttpHealthCheck("/status/readiness")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
    .WaitFor(otelCollector);

// Service References (for inter-service communication)
api.WithReference(grpc)
   .WithReference(graphql);

builder.Build().Run();
```

---

## Appendix B: VictoriaMetrics Alternative (Docker Compose)

As an alternative to Aspire container management, VictoriaMetrics stack can be run via Docker Compose:

```yaml
# docker-compose.victoria.yml
version: '3.8'

services:
  victoriametrics:
    image: victoriametrics/victoria-metrics:latest
    ports:
      - "8428:8428"
    volumes:
      - victoria-data:/victoria-metrics-data
    command:
      - '-storageDataPath=/victoria-metrics-data'
      - '-retentionPeriod=7d'

  victorialogs:
    image: victoriametrics/victoria-logs:latest
    ports:
      - "9428:9428"
    volumes:
      - victoria-logs-data:/victoria-logs-data

  victoriatraces:
    image: victoriametrics/victoria-traces:latest
    ports:
      - "14268:14268"  # Jaeger ingest
      - "16686:16686"  # Jaeger UI

  otel-collector:
    image: otel/opentelemetry-collector-contrib:latest
    ports:
      - "4317:4317"   # OTLP gRPC
      - "4318:4318"   # OTLP HTTP
    volumes:
      - ./otel-collector-config.yaml:/etc/otelcol/config.yaml
    depends_on:
      - victoriametrics
      - victorialogs
      - victoriatraces

volumes:
  victoria-data:
  victoria-logs-data:
```

**Usage:**
```bash
# Start VictoriaMetrics stack
docker-compose -f docker-compose.victoria.yml up -d

# Run Aspire (which will use the already-running stack)
cd hive.microservices/demo/Hive.MicroServices.Demo.Aspire
dotnet run
```
