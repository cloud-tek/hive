# Hive Documentation Index

This is the central index for all Hive documentation.
For the project overview and quick-start, see the [root README](../README.md).

---

## Contents

- [Getting Started](#getting-started)
- [Build & CI Reference](#build--ci-reference)
- [Design Documents](#design-documents)
- [Plans](#plans)
- [Topic Guides](#topic-guides)
- [Package & Extension READMEs](#package--extension-readmes)

---

## Getting Started

See [getting-started.md](getting-started.md) for a step-by-step guide to building your first Hive service.

The root [README Quick Start](../README.md#quick-start) also covers basic REST API and configuration examples.

---

## Build & CI Reference

See [build.md](build.md) for the full reference of `cloudtek-build` targets and `dotnet` CLI equivalents.

**Quick reference:**

```bash
# Full build (all targets, including checks)
dotnet tool run cloudtek-build --target All

# Quick build (skip checks — useful during development)
dotnet tool run cloudtek-build --target All --Skip RunChecks

# Build with dotnet CLI
dotnet build Hive.sln

# Run all tests
dotnet test Hive.sln

# Run tests by category
dotnet test --filter Category=UnitTests
dotnet test --filter Category=IntegrationTests
dotnet test --filter Category=ModuleTests
dotnet test --filter Category=SmokeTests
dotnet test --filter Category=SystemTests
```

---

## Design Documents

Documents in [`design/`](design/) capture architectural decisions and analysis.

| Document | Description |
|---|---|
| [aspire_integration_design.md](design/aspire_integration_design.md) | Draft design for integrating Hive with .NET Aspire orchestration |
| [cors_extraction_analysis.md](design/cors_extraction_analysis.md) | Analysis of whether CORS should be extracted to `Hive.Abstractions` for cross-host reuse |
| [custom_endpoints_seam_design.md](design/custom_endpoints_seam_design.md) | Accepted design for the `MapEndpoints` extension seam (PR-3, 10.2.0 milestone) |
| [hive_functions_design.md](design/hive_functions_design.md) | Draft design for the Azure Functions integration (`Hive.Functions`) |
| [otel_configuration_strategy.md](design/otel_configuration_strategy.md) | Strategy for declarative OpenTelemetry configuration via `appsettings.json` |

---

## Plans

Documents in [`plan/`](plan/) track implementation status and handoff notes.

| Document | Description |
|---|---|
| [open_issues_implementation_plan.md](plan/open_issues_implementation_plan.md) | Consolidated implementation plan covering issues #62, #64, #65, #66 and their release groupings |
| [pr1_logger_nre_handoff.md](plan/pr1_logger_nre_handoff.md) | Handoff note for the Logger NRE bug fix shipped in 10.1.1 (PR-1 of issue #64) |

---

## Topic Guides

Focused deep-dives on specific concerns.

| Document | Description |
|---|---|
| [dagger.md](dagger.md) | Workaround for the 128 MB `File.Contents()` limit in the Dagger-based CI validate workflow |
| [healthchecks.md](healthchecks.md) | Design notes for the threshold-based readiness gating system in `Hive.HealthChecks` |
| [http.md](http.md) | Design and usage guide for `Hive.HTTP` typed HTTP clients (Refit, resilience, telemetry) |
| [messaging.md](messaging.md) | Design notes for the `Hive.Messaging` extension built on Wolverine |
| [optimization.md](optimization.md) | Code duplication and optimization analysis for `Hive.HealthChecks` |
| [otel-aspire.md](otel-aspire.md) | Implemented guide for OpenTelemetry Collector integration with Aspire and VictoriaMetrics |

---

## Package & Extension READMEs

| Package | Purpose |
|---|---|
| [Hive.Abstractions & Hive.Testing](../hive.core/readme.md) | Foundation layer — core interfaces, extension base classes, xUnit test attributes |
| [Hive.MicroServices](../hive.microservices/src/Hive.MicroServices/README.md) | Core ASP.NET Core orchestration framework — lifecycle, pipeline modes, Kubernetes probes |
| [Hive.MicroServices.Api](../hive.microservices/README.md#hive.microservicesapi) | REST API support — minimal APIs and MVC controllers |
| [Hive.MicroServices.GraphQL](../hive.microservices/README.md#hive.microservicesgraphql) | GraphQL API support via HotChocolate |
| [Hive.MicroServices.Grpc](../hive.microservices/README.md#hive.microservicesgrpc) | gRPC service support (protobuf-first) |
| [Hive.MicroServices.Mcp](../hive.microservices/src/Hive.MicroServices.Mcp/README.md) | MCP (Model Context Protocol) server support — expose tools/data to LLM clients |
| [Hive.MicroServices.Job](../hive.microservices/README.md#hive.microservicesjob) | Background worker and scheduled job support |
| [Hive.MicroServices.Testing](../hive.microservices/src/Hive.MicroServices.Testing/README.md) | TestServer integration testing utilities |
| [CORS](../hive.microservices/src/Hive.MicroServices/CORS/README.md) | CORS configuration guide — policies, security best practices, validation |
| [Hive.HTTP](../hive.extensions/src/Hive.HTTP/README.md) | Typed HTTP clients with Refit, resilience, and OpenTelemetry instrumentation |
| [Hive.HealthChecks](../hive.extensions/src/Hive.HealthChecks/README.md) | Threshold-based readiness gating with background health monitoring |
| [Hive.Messaging](../hive.extensions/src/Hive.Messaging/README.md) | Wolverine-based messaging with RabbitMQ transport and readiness middleware |
| [Hive.OpenTelemetry](../hive.opentelemetry/README.md) | Unified logs, traces, and metrics via OTLP — zero-config defaults |
| [Hive.Functions](../hive.functions/README.md) | Azure Functions Worker integration following the Hive extension pattern |
| [hive.extensions module](../hive.extensions/README.md) | Module-level README covering HTTP, Messaging, and HealthChecks together |