# OpenTelemetry Collector Integration for Aspire + VictoriaMetrics

**Status**: Implemented
**Date**: 2026-02-20
**Author**: Architecture Team
**Supersedes**: ASPIRE_INTEGRATION_DESIGN.md Phase 5 (VictoriaMetrics Integration)

---

## Problem Statement

Aspire automatically injects `OTEL_EXPORTER_OTLP_ENDPOINT` into every orchestrated project, pointing to its own in-memory OTLP receiver. This is how traces, logs, and metrics appear in the Aspire dashboard.

Overriding this environment variable (e.g., via `WithEnvironment`) to target an external backend like VictoriaMetrics **breaks Aspire dashboard telemetry entirely** — the dashboard shows no data.

### Why Direct Environment Variable Override Fails

Three compounding issues make the direct override approach unworkable:

**1. Aspire dashboard loses all telemetry**

Setting `OTEL_EXPORTER_OTLP_ENDPOINT` to VictoriaMetrics replaces Aspire's auto-injected endpoint. The standard OTEL SDK only supports one OTLP exporter per signal, so telemetry flows to VictoriaMetrics exclusively.

**2. Hive's OTEL extension uses a single endpoint for all signals**

`Hive.OpenTelemetry` resolves one endpoint via `ResolveOtlpEndpoint()` and applies it programmatically to all three signal exporters (metrics, traces, logs) using `AddOtlpExporter(o => { o.Endpoint = ... })`.

Per the .NET OTEL SDK, programmatic delegates **always override** environment variables. This means signal-specific env vars (`OTEL_EXPORTER_OTLP_METRICS_ENDPOINT`, `OTEL_EXPORTER_OTLP_TRACES_ENDPOINT`, `OTEL_EXPORTER_OTLP_LOGS_ENDPOINT`) are ignored — all signals go to the same endpoint.

Since VictoriaMetrics stack exposes separate services for metrics (`:8428`), traces (`:10428`), and logs (`:9428`), a single endpoint cannot address all three.

**3. Protocol mismatch**

Setting `OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf` via environment variable has no effect because Hive's extension reads the protocol from `IConfiguration` (`OpenTelemetry:Otlp:Protocol`), defaulting to gRPC. gRPC requests against VictoriaMetrics HTTP endpoints fail.

---

## Solution: OpenTelemetry Collector as Intermediary

The officially recommended approach by both the [OpenTelemetry project](https://opentelemetry.io/docs/collector/) and [VictoriaMetrics documentation](https://docs.victoriametrics.com/guides/getting-started-with-opentelemetry/) is to deploy an OpenTelemetry Collector between applications and backends.

### Why This Works

- Applications emit OTLP to a **single** Collector endpoint — compatible with Hive's single-endpoint model
- The Collector fans out to **multiple** backends per signal using native pipeline architecture
- Aspire dashboard remains a backend alongside VictoriaMetrics
- No changes required to `Hive.OpenTelemetry` extension
- Mirrors production deployment topology

### Why Not a NuGet Package

Two community packages exist for adding an OTel Collector to Aspire:

- [practical-otel/opentelemetry-aspire-collector](https://github.com/practical-otel/opentelemetry-aspire-collector) (`PracticalOtel.OtelCollector.Aspire`) — still in prerelease (0.9.x-rc), targets net8.0
- [wertzui/Aspire.Hosting.OpenTelemetryCollector](https://github.com/wertzui/Aspire.Hosting.OpenTelemetryCollector) (`OpenTelemetryCollector.Aspire.Hosting`) — v1.0.0, targets net8.0, different API from practical-otel

Neither is stable on net10.0. The implementation uses `builder.AddContainer()` directly — this is all the packages do under the hood, and it avoids a dependency on pre-release or incompatible packages.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  Hive.MicroServices.Demo.Aspire (AppHost)                   │
│  .WithOtelCollector() sets OTEL_EXPORTER_OTLP_ENDPOINT      │
│  on each service to target the Collector                     │
└─────────────────────────────────────────────────────────────┘
                          │
         ┌────────────────┼────────────────┐
         ▼                ▼                ▼
    ┌─────────┐     ┌─────────┐     ┌─────────┐
    │  Api    │     │ GraphQL │     │  gRPC   │  ... all services
    └─────────┘     └─────────┘     └─────────┘
         │                │                │
         └────────────────┴────────────────┘
                          │ OTLP (gRPC :4317)
                          ▼
              ┌───────────────────────┐
              │  OpenTelemetry        │
              │  Collector            │
              │  (fan-out consumer)   │
              └───────────────────────┘
                          │
         ┌────────────────┼────────────────┐
         ▼                ▼                ▼
    ┌─────────┐     ┌──────────┐     ┌─────────┐
    │ Aspire  │     │ Victoria │     │ Victoria│
    │Dashboard│     │ Metrics  │     │ Traces  │
    │(otlp)   │     │ :8428    │     │ :10428  │
    └─────────┘     └──────────┘     └─────────┘
                          │
                     ┌──────────┐
                     │ Victoria │
                     │  Logs    │
                     │  :9428   │
                     └──────────┘
```

---

## Implementation

### 1. Collector Configuration File

`hive.microservices/demo/Hive.MicroServices.Demo.Aspire/otel-collector-config.yaml`:

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

exporters:
  # Fan-out: Aspire Dashboard (env vars injected by AppHost)
  otlp/aspire:
    endpoint: ${env:ASPIRE_ENDPOINT}
    headers:
      x-otlp-api-key: ${env:ASPIRE_API_KEY}
    tls:
      insecure: true

  # Fan-out: VictoriaMetrics — metrics on port 8428
  otlphttp/victoriametrics:
    endpoint: http://victoria-metrics:8428/opentelemetry
    tls:
      insecure: true

  # Fan-out: VictoriaLogs — logs on port 9428
  otlphttp/victorialogs:
    endpoint: http://victoria-logs:9428/insert/opentelemetry
    tls:
      insecure: true

  # Fan-out: VictoriaTraces — traces on port 10428
  otlphttp/victoriatraces:
    traces_endpoint: http://victoria-traces:10428/insert/opentelemetry/v1/traces
    tls:
      insecure: true

service:
  pipelines:
    traces:
      receivers: [otlp]
      exporters: [otlp/aspire, otlphttp/victoriatraces]
    metrics:
      receivers: [otlp]
      exporters: [otlp/aspire, otlphttp/victoriametrics]
    logs:
      receivers: [otlp]
      exporters: [otlp/aspire, otlphttp/victorialogs]
```

### 2. AppHost Configuration

The AppHost reads the Aspire dashboard OTLP endpoint from configuration, translates `localhost` to `host.docker.internal` for container access, and passes it to the Collector via environment variables.

```csharp
// Resolve Aspire dashboard OTLP endpoint for collector forwarding
var dashboardOtlpUrl = builder.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"]
  ?? builder.Configuration["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"]
  ?? "http://localhost:18889";

var dashboardEndpointForContainer = dashboardOtlpUrl
  .Replace("localhost", "host.docker.internal")
  .Replace("127.0.0.1", "host.docker.internal")
  .Replace("[::1]", "host.docker.internal");

// OpenTelemetry Collector — fans out to Aspire dashboard + VictoriaMetrics stack
var otelCollector = builder.AddContainer("otel-collector",
    "otel/opentelemetry-collector-contrib")
  .WithBindMount("otel-collector-config.yaml", "/config/otel-collector-config.yaml")
  .WithArgs("--config=/config/otel-collector-config.yaml")
  .WithEndpoint(targetPort: 4317, name: "grpc", scheme: "http")
  .WithEndpoint(targetPort: 4318, name: "http", scheme: "http")
  .WithEnvironment("ASPIRE_ENDPOINT", dashboardEndpointForContainer)
  .WithEnvironment("ASPIRE_API_KEY", builder.Configuration["AppHost:OtlpApiKey"]);
```

### 3. Service Wiring

Each service uses `.WithOtelCollector(otelCollector)` which sets `OTEL_EXPORTER_OTLP_ENDPOINT` to the Collector's gRPC endpoint:

```csharp
builder.AddProject<Projects.Hive_MicroServices_Demo_Api>("hive-microservices-demo-api")
  .WithHttpHealthCheck("/status/readiness")
  .WithOtelCollector(otelCollector);
```

The extension method is defined in `AspireExtensions.cs`:

```csharp
public static IResourceBuilder<T> WithOtelCollector<T>(
    this IResourceBuilder<T> builder,
    IResourceBuilder<ContainerResource> otelCollector)
    where T : IResourceWithEnvironment
{
    return builder
        .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("grpc"));
}
```

### 4. How Aspire Dashboard Forwarding Works

1. AppHost reads `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL` from its own configuration (set in `launchSettings.json`)
2. `localhost` is replaced with `host.docker.internal` so containers can reach the host
3. The translated URL is injected as `ASPIRE_ENDPOINT` into the Collector container
4. The Collector config references `${env:ASPIRE_ENDPOINT}` in the `otlp/aspire` exporter
5. The `AppHost:OtlpApiKey` is injected as `ASPIRE_API_KEY` for authentication

---

## Telemetry Flow Summary

| Signal  | App → Collector | Collector → Aspire Dashboard | Collector → VictoriaMetrics Stack |
|---------|-----------------|------------------------------|-----------------------------------|
| Traces  | OTLP/gRPC :4317 | OTLP/gRPC via `otlp/aspire`  | OTLP/HTTP via `otlphttp/victoriatraces` |
| Metrics | OTLP/gRPC :4317 | OTLP/gRPC via `otlp/aspire`  | OTLP/HTTP via `otlphttp/victoriametrics` |
| Logs    | OTLP/gRPC :4317 | OTLP/gRPC via `otlp/aspire`  | OTLP/HTTP via `otlphttp/victorialogs` |

The Collector's fan-out consumer sends a copy of every telemetry item to each exporter independently. A failure in one exporter does not block others.

---

## Hive.OpenTelemetry Compatibility

No changes are required to `Hive.OpenTelemetry`. The extension resolves a single `OTEL_EXPORTER_OTLP_ENDPOINT` and exports all signals to it via gRPC — which is exactly what the Collector expects on `:4317`. The Collector then handles per-signal routing and protocol translation (gRPC → HTTP/protobuf for VictoriaMetrics).

---

## Files

| File | Purpose |
|------|---------|
| `hive.microservices/demo/Hive.MicroServices.Demo.Aspire/AppHost.cs` | Orchestrator with Collector + Victoria stack |
| `hive.microservices/demo/Hive.MicroServices.Demo.Aspire/AspireExtensions.cs` | `WithOtelCollector` extension method |
| `hive.microservices/demo/Hive.MicroServices.Demo.Aspire/otel-collector-config.yaml` | Collector pipeline config with fan-out |

---

## References

- [OpenTelemetry Collector Architecture](https://opentelemetry.io/docs/collector/)
- [practical-otel/opentelemetry-aspire-collector (GitHub)](https://github.com/practical-otel/opentelemetry-aspire-collector)
- [VictoriaMetrics OpenTelemetry Guide](https://docs.victoriametrics.com/guides/getting-started-with-opentelemetry/)
- [VictoriaMetrics Full-Stack Observability with OTEL](https://victoriametrics.com/blog/victoriametrics-full-stack-observability-otel-demo/)
- [.NET Aspire Telemetry](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/telemetry)
- [dotnet/aspire#11298 — OTEL_EXPORTER_OTLP_ENDPOINT override issue](https://github.com/dotnet/aspire/issues/11298)
- [open-telemetry/opentelemetry-dotnet#4043 — Multiple exporter configuration](https://github.com/open-telemetry/opentelemetry-dotnet/issues/4043)
- [Aspire Integration Design](ASPIRE_INTEGRATION_DESIGN.md)
