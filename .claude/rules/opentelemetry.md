# opentelemetry.md

This file provides guidance to Claude Code (claude.ai/code) about the Hive OpenTelemetry integration.

## OpenTelemetry Integration

The `Hive.OpenTelemetry` extension provides unified observability. The only extension point on the host is the list of additional activity sources; everything else is configured through `IConfiguration`.

```csharp
var service = new MicroService("service-name")
    .WithOpenTelemetry()
    .ConfigureApiPipeline(app => { });

// Subscribe to extra ActivitySources
var service = new MicroService("service-name")
    .WithOpenTelemetry(additionalActivitySources: ["MyApp"])
    .ConfigureApiPipeline(app => { });
```

Signature: `WithOpenTelemetry<THost>(this THost host, IEnumerable<string>? additionalActivitySources = null) where THost : IMicroServiceCore`. There are no `logging:`/`tracing:`/`metrics:` lambda parameters. Register it before the `Configure*Pipeline` call.

**Configuration** comes from the `OpenTelemetry` section (`OpenTelemetryOptions`):

```json
{
  "OpenTelemetry": {
    "Otlp": { "Endpoint": "http://localhost:4317" },
    "Tracing": { "EnableHttpClientInstrumentation": false }
  }
}
```

**OTLP endpoint resolution order:**
1. `OpenTelemetry:Otlp:Endpoint`
2. Environment variable `OTEL_EXPORTER_OTLP_ENDPOINT`

When an endpoint resolves (or the signal's `EnableOtlpExporter` is `true`), the OTLP exporters are enabled. Only logging has a console exporter (`OpenTelemetry:Logging:EnableConsoleExporter`, default `true`); traces and metrics are not exported unless OTLP is enabled.

**Resource Attributes:**
- `service.name` - From `IMicroService.Name`
- `service.instance.id` - From `IMicroService.Id`

**Instrumentation Includes:**
- ASP.NET Core (requests, exceptions)
- HTTP client (outbound calls)
- Runtime metrics (GC, thread pool, etc.)
- Activity sources: auto-discovered from extensions implementing `IActivitySourceProvider` (e.g., Wolverine via `Hive.Messaging`) plus those passed in `additionalActivitySources`
- No custom meters are subscribed