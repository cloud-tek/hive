# opentelemetry.md

This file provides guidance to Claude Code (claude.ai/code) about the Hive OpenTelemetry integration.

## OpenTelemetry Integration

The `Hive.OpenTelemetry` extension provides unified observability:

```csharp
var service = new MicroService("service-name")
    .WithOpenTelemetry(
        logging: builder => { /* customize logging */ },
        tracing: builder => { /* customize tracing */ },
        metrics: builder => { /* customize metrics */ }
    );
```

**Environment Variables:**
- `OTEL_EXPORTER_OTLP_ENDPOINT` - OTLP collector endpoint (e.g., `http://localhost:4317`)

When the endpoint is set, telemetry is exported via OTLP; otherwise, console export is used.

**Resource Attributes:**
- `service.name` - From `IMicroService.Name`
- `service.instance.id` - From `IMicroService.Id`

**Instrumentation Includes:**
- ASP.NET Core (requests, exceptions)
- HTTP client (outbound calls)
- Runtime metrics (GC, thread pool, etc.)
- Auto-discovered activity sources from extensions implementing `IActivitySourceProvider` (e.g., Wolverine via `Hive.Messaging`)