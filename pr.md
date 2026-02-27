## Summary

Add the `Hive.HealthChecks` module — an application-level health check framework that provides threshold-based readiness gating, per-check timeouts, and distributed tracing for Hive microservices.

### Core Abstractions (`Hive.Abstractions`)

- `IHiveHealthCheck` — interface with C# 11 static abstract members (`CheckName`, `ConfigureDefaults`)
- `HiveHealthCheckOptions` — per-check configuration (thresholds, timeout, readiness behavior)
- `HealthCheckStatus` — `Unknown`, `Healthy`, `Degraded`, `Unhealthy`
- `ReadinessThreshold` — controls whether `Degraded` counts as passing (`Degraded` or `Healthy`)
- `HealthCheckStateSnapshot` — immutable snapshot of check state exposed via `IHealthCheckStateProvider`

### Extension Implementation (`Hive.HealthChecks`)

- **Fluent builder API** — `WithHealthChecks(builder => builder.WithHealthCheck<T>())` with explicit-only registration (no auto-discovery)
- **Three-tier configuration priority** — POCO defaults < `IConfiguration` < Builder (explicit code)
- **`HealthCheckStartupService`** — blocks readiness probe until critical checks pass; binds `HiveHealthCheck<TOptions>.Options` from `IConfiguration`
- **`HealthCheckBackgroundService`** — eager initial evaluation followed by independent `PeriodicTimer` loops per check, with per-check timeout via linked `CancellationTokenSource`
- **`HealthCheckRegistry`** — thread-safe state machine tracking consecutive failures/successes, computing `IsPassingForReadiness` against configurable thresholds
- **`ReflectionBridge`** — bridges runtime `Type` objects to static abstract member invocation on `IHiveHealthCheck`
- **`IActivitySourceProvider`** — exposes `Hive.HealthChecks` activity source, auto-discovered by `Hive.OpenTelemetry`

### Readiness Middleware Integration

- `ReadinessMiddleware` now consults `IHealthCheckStateProvider` alongside `IMicroService.IsReady`
- `ReadinessResponse` includes per-check detail entries (status, duration, thresholds, consecutive counts)
- Backward compatible — `null` checks list when no health checks are registered

### Concrete Implementation

- `RabbitMqHealthCheck` — wraps upstream `HealthChecks.RabbitMQ`, reads connection URI from `IConfiguration`, fail-fast with descriptive exception on missing config

### Messaging Cleanup

- Removed `ConfigureHealthChecks` from `MicroServiceExtension`, `IMessagingTransportProvider`, `MessagingExtensionBase`, and `RabbitMqTransportProvider` — health checks are now registered explicitly via `Hive.HealthChecks` instead of being wired through the messaging transport

### Documentation

- Comprehensive module README at `hive.extensions/src/Hive.HealthChecks/README.md` (lifecycle diagrams, configuration reference, threshold explanations, options binding guide)
- Updated `hive.extensions/README.md` with architecture diagram, module entry, and package table
- Design document at `docs/healthchecks.md`

### Test Suite (69 tests)

| File | Tests | Coverage |
|------|-------|---------|
| `HealthCheckRegistryTests` | 30 | Registry state machine: register, update, counters, failure/success thresholds, readiness thresholds, affects-readiness, thread safety |
| `HealthCheckStartupServiceTests` | 12 | Registration, blocking evaluation (healthy/unhealthy/throws/non-blocking), configuration overrides with Theory |
| `HealthChecksBuilderTests` | 7 | Fluent API, duplicate detection, interval nullable |
| `HealthChecksExtensionTests` | 7 | Three-tier interval priority, DI wiring, activity source |
| `ReflectionBridgeTests` | 3 | Static abstract invocation via reflection |
| `HealthCheckBackgroundServiceTests` | 4 | Eager evaluation, timeout, exception handling, configuration overrides |
| `StartupTests` | 3 | `WithHealthChecks()` extension method |
| `HiveHealthCheckOptionsTests` | 1 | Default values validation |
| **Test doubles** | — | `FakeHealthCheck` (configurable delegate), `AlternativeFakeHealthCheck`, `FakeHealthCheckWithOptions` |

## Test plan

- [x] `dotnet build Hive.sln` — 0 errors, 0 warnings
- [x] `dotnet tool run cloudtek-build --target All` — all 21 targets pass (including `FormatCheck`, `FormatAnalyzersCheck`)
- [x] `dotnet test Hive.sln` — 395 tests pass (69 new + 326 existing), 0 failures
- [ ] Verify demo apps start with health checks enabled (`Hive.MicroServices.Demo`, `Hive.MicroServices.Demo.ApiControllers`)
- [ ] Verify `/readiness` endpoint returns check details in JSON response
- [ ] Verify RabbitMQ health check blocks startup when broker is unreachable
