# opentelemetry.md

This file provides guidance to Claude Code (claude.ai/code) about the Hive OpenTelemetry integration.

## OpenTelemetry Integration

The `Hive.OpenTelemetry` extension provides unified observability. `WithOpenTelemetry` takes no logging/tracing/metrics lambdas; its only parameter is an optional list of additional activity sources:

```csharp
var service = new MicroService("service-name")
    .WithOpenTelemetry();

var service = new MicroService("service-name")
    .WithOpenTelemetry(additionalActivitySources: ["MyApp.Orders"]);
```

Behaviour is driven by the optional `OpenTelemetry` configuration section, with the subsections `Resource`, `Logging`, `Tracing`, `Metrics` and `Otlp`.

**OTLP Endpoint:**
Resolved in this order: `OpenTelemetry:Otlp:Endpoint`, then the `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable (e.g., `http://localhost:4317`), then none.

A signal (logs, traces, metrics) gets the OTLP exporter when an endpoint is resolved or when that signal's `EnableOtlpExporter` is true. In the latter case an endpoint must also be configured.

**Console Exporter:**
The console exporter exists only for logs (`OpenTelemetry:Logging:EnableConsoleExporter`, default true). Setting OTLP does NOT turn it off. Traces and metrics have no console exporter.

**Resource Attributes:**
- `service.name` - From `IMicroService.Name`
- `service.instance.id` - From `IMicroService.Id`
- `service.namespace` / `service.version` - From optional `Resource:ServiceNamespace` / `Resource:ServiceVersion`
- Custom attributes from `Resource:Attributes:*` are added; reserved keys are skipped

**Instrumentation Includes** (each toggled by `Tracing:Enable*` / `Metrics:Enable*`, default true):
- Tracing: ASP.NET Core and HTTP client
- Metrics: ASP.NET Core, HTTP client and runtime (GC, thread pool, etc.)
- Activity sources auto-discovered from extensions implementing `IActivitySourceProvider` (e.g., Wolverine via `Hive.Messaging`), plus anything passed in `additionalActivitySources`

Meters from other extensions (e.g., `Hive.Messaging`, `Hive.HTTP`) are NOT auto-subscribed.

**Further Customisation:**
Sampling, extra meters and extra exporters go through the additive OpenTelemetry SDK hooks, called inside Hive's `ConfigureServices`. They add to Hive's defaults and never replace them:

```csharp
service.ConfigureServices((services, _) =>
{
  services.ConfigureOpenTelemetryTracerProvider((_, tracing) => tracing.SetSampler(new TraceIdRatioBasedSampler(0.1)));
  services.ConfigureOpenTelemetryMeterProvider((_, metrics) => metrics.AddMeter("Hive.Messaging"));
});
```
