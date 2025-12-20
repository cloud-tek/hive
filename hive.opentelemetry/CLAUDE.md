# hive.opentelemetry/CLAUDE.md

This file provides guidance for developing & testing the Hive.OpenTelemetry module.

## Testing Strategy for hive.opentelemetry

### Test Project Location

**Path:** `hive.opentelemetry/tests/Hive.OpenTelemetry.Tests/`

Following the repository structure policy, all tests are located within the module's `tests/` subfolder.

---

## Test Categories

### 1. Extension Registration Tests ✅ IMPLEMENTED

**File:** `ExtensionTests.cs`

Tests for the `WithOpenTelemetry` extension method registration:

- [x] Verify extension is properly added to service.Extensions collection
- [x] Verify extension is of type `Hive.OpenTelemetry.Extension`
- [x] Verify fluent API returns the IMicroService instance for chaining
- [x] Verify custom environment variable name can be passed
- [x] Verify custom logging configuration parameter
- [x] Verify custom tracing configuration parameter
- [x] Verify custom metrics configuration parameter
- [x] Verify all custom configurations together
- [x] Verify multiple extension registrations
- [x] Verify extension chaining with other service configurations

**Status:** 9/9 tests passing

---

### 2. Resource Configuration Tests

**File:** `OpenTelemetryTests.Resources.cs` (planned)

Tests for OpenTelemetry resource attribute configuration:

- [ ] Verify `service.name` is set from `IMicroService.Name`
- [ ] Verify `service.instance.id` is set from `IMicroService.Id`
- [ ] Verify `serviceNamespace` is null (as per current implementation)
- [ ] Verify `serviceVersion` is null (as per current implementation)
- [ ] Verify `autoGenerateServiceInstanceId` is false
- [ ] Verify resource attributes are properly propagated to logs, traces, and metrics

---

### 3. Logging Configuration Tests

**File:** `OpenTelemetryTests.Logging.cs` (planned)

#### Default logging configuration (no custom action)

- [ ] Verify console exporter is added by default
- [ ] Verify OTLP exporter is added when `OTEL_EXPORTER_OTLP_ENDPOINT` env var is set
- [ ] Verify OTLP exporter uses correct endpoint from environment variable
- [ ] Verify console exporter remains when OTLP endpoint is configured
- [ ] Verify no OTLP exporter when environment variable is not set

#### Custom logging configuration

- [ ] Verify custom `Action<LoggerProviderBuilder>` is invoked
- [ ] Verify custom configuration overrides default behavior
- [ ] Verify service starts with custom logging configuration

---

### 4. Tracing Configuration Tests

**File:** `OpenTelemetryTests.Tracing.cs` (planned)

#### Default tracing configuration (no custom action)

- [ ] Verify ASP.NET Core instrumentation is added
- [ ] Verify HTTP Client instrumentation is added
- [ ] Verify OTLP exporter is added when `OTEL_EXPORTER_OTLP_ENDPOINT` env var is set
- [ ] Verify OTLP exporter uses correct endpoint from environment variable
- [ ] Verify no OTLP exporter when environment variable is not set

#### Custom tracing configuration

- [ ] Verify custom `Action<TracerProviderBuilder>` is invoked
- [ ] Verify custom configuration overrides default behavior
- [ ] Verify service starts with custom tracing configuration

---

### 5. Metrics Configuration Tests

**File:** `OpenTelemetryTests.Metrics.cs` (planned)

#### Default metrics configuration (no custom action)

- [ ] Verify ASP.NET Core instrumentation is added
- [ ] Verify HTTP Client instrumentation is added
- [ ] Verify Runtime instrumentation is added
- [ ] Verify OTLP exporter is added when `OTEL_EXPORTER_OTLP_ENDPOINT` env var is set
- [ ] Verify OTLP exporter uses correct endpoint from environment variable
- [ ] Verify no OTLP exporter when environment variable is not set

#### Custom metrics configuration

- [ ] Verify custom `Action<MeterProviderBuilder>` is invoked
- [ ] Verify custom configuration overrides default behavior
- [ ] Verify service starts with custom metrics configuration

---

### 6. Environment Variable Tests

**File:** `OpenTelemetryTests.Environment.cs` (planned)

#### OTEL_EXPORTER_OTLP_ENDPOINT handling

- [ ] Verify endpoint is read from environment variables correctly
- [ ] Verify endpoint is properly parsed as URI
- [ ] Verify behavior when environment variable is missing
- [ ] Verify behavior when environment variable is empty string
- [ ] Verify behavior with malformed URIs (should throw or handle gracefully)
- [ ] Verify behavior with different URI schemes (http, https)

#### Custom environment variable name

- [ ] Verify custom environment variable name parameter works
- [ ] Verify extension reads from custom variable instead of default

**Testing Utilities:**
- Use `EnvironmentVariableScope` from `Hive.Testing` for scoped environment variable manipulation

---

### 7. Service Lifecycle Integration Tests

**File:** `MicroServiceTests.Startup.cs` (planned)

#### Service startup with OpenTelemetry

- [ ] Verify service starts successfully with OpenTelemetry configured
- [ ] Verify service starts with Api pipeline mode
- [ ] Verify service starts with ApiControllers pipeline mode
- [ ] Verify service starts with GraphQL pipeline mode
- [ ] Verify service starts with Grpc pipeline mode
- [ ] Verify service starts with Job pipeline mode
- [ ] Verify service starts with None pipeline mode (default)
- [ ] Verify OpenTelemetry doesn't prevent service startup
- [ ] Verify service can start without OTLP endpoint configured (console-only mode)

#### Service with multiple extensions

- [ ] Verify OpenTelemetry works alongside other extensions
- [ ] Verify order of extension registration doesn't break functionality

**Testing Utilities:**
- Use `.InTestClass<T>()` extension for test isolation
- Use `.ShouldStart()` extension for startup verification
- Use `.ShouldFailToStart()` for negative test cases

---

### 8. Configuration Section Tests

**File:** `OpenTelemetryTests.Constants.cs` (planned)

Tests for configuration constants:

- [ ] Verify `Constants.Environment.OtelExporterOtlpEndpoint` has correct value ("OTEL_EXPORTER_OTLP_ENDPOINT")
- [ ] Verify `Constants.OtelLoggingExporterSection` has correct value ("OpenTelemetry:Logging")
- [ ] Verify `Constants.OtelTracingExporterSection` has correct value ("OpenTelemetry:Tracing")
- [ ] Verify `Constants.OtelMetricsExporterSection` has correct value ("OpenTelemetry:Metrics")

**Note:** Configuration sections are currently defined but not used in the implementation. Document intended usage.

---

### 9. Pipeline Mode Compatibility Tests

**File:** `MicroServiceTests.PipelineModes.cs` (planned)

Tests for OpenTelemetry compatibility with different pipeline modes:

- [ ] Test with `ConfigureApiPipeline` - minimal API endpoints
- [ ] Test with `ConfigureApiControllerPipeline` - controller-based APIs
- [ ] Test with `ConfigureGraphQLPipeline` - GraphQL APIs
- [ ] Test with `ConfigureGrpcPipeline` - gRPC services
- [ ] Test with `ConfigureCodeFirstGrpcPipeline` - code-first gRPC
- [ ] Test with `ConfigureDefaultServicePipeline` - None mode
- [ ] Verify telemetry is emitted for each pipeline mode

---

### 10. Error Handling Tests

**File:** `OpenTelemetryTests.ErrorHandling.cs` (planned)

Tests for invalid configuration scenarios:

- [ ] Verify behavior with null MicroService instance (should not be possible)
- [ ] Verify behavior with invalid OTLP endpoint URIs
- [ ] Verify error messages are clear and actionable
- [ ] Verify service handles OTLP export failures gracefully
- [ ] Verify service continues operation if OTLP endpoint is unreachable

---

### 11. End-to-End Observability Tests (Optional)

**File:** `OpenTelemetryTests.E2E.cs` (planned)

Integration tests for actual telemetry emission:

- [ ] Verify logs are emitted to console
- [ ] Verify traces are created for HTTP requests
- [ ] Verify metrics are collected for runtime
- [ ] Use in-memory exporters to validate telemetry data structure
- [ ] Verify correlation between logs, traces, and metrics (resource attributes)
- [ ] Verify activity context propagation across service boundaries

**Implementation Notes:**
- Use `InMemoryExporter` for logs, traces, and metrics
- Verify exported data contains expected resource attributes
- Verify instrumentation captures expected signals

---

### 12. Demo Application Validation

**File:** System/Smoke tests in demo project (planned)

Tests for demo application integration:

- [ ] Verify demo application starts with OpenTelemetry
- [ ] Verify demo can make HTTP requests and generate telemetry
- [ ] Verify demo works with and without OTLP endpoint
- [ ] Verify demo emits logs, traces, and metrics

**Demo Location:** `hive.microservices/demo/Hive.MicroServices.Demo.Api/`

---

## Testing Utilities

### From Hive.Testing

- **`[UnitTest]`** - Attribute for unit tests (Category: "UnitTests")
- **`[IntegrationTest]`** - Attribute for integration tests (Category: "IntegrationTests")
- **`[SmokeTest]`** - Attribute for smoke tests (Category: "SmokeTests")
- **`[SystemTest]`** - Attribute for system tests (Category: "SystemTests")
- **`EnvironmentVariableScope`** - Scoped environment variable manipulation for tests
- **`.InTestClass<T>()`** - Extension for test isolation
- **`.ShouldStart()`** - Extension for startup verification
- **`.ShouldFailToStart()`** - Extension for negative startup tests

### Running Tests

```bash
# Run all OpenTelemetry tests
dotnet test hive.opentelemetry/tests/Hive.OpenTelemetry.Tests/

# Run specific test category
dotnet test --filter Category=UnitTests
dotnet test --filter Category=IntegrationTests

# Run specific test class
dotnet test --filter FullyQualifiedName~ExtensionTests

# Run specific test
dotnet test --filter FullyQualifiedName~ExtensionTests.GivenWithOpenTelemetry_WhenCalled_ThenExtensionIsAddedToServiceExtensions
```

---

## Test Implementation Progress

| Category | File | Tests Planned | Tests Implemented | Status |
|----------|------|---------------|-------------------|--------|
| Extension Registration | `ExtensionTests.cs` | 9 | 9 | ✅ Complete |
| Resource Configuration | `OpenTelemetryTests.Resources.cs` | 6 | 0 | ⏳ Planned |
| Logging Configuration | `OpenTelemetryTests.Logging.cs` | 8 | 0 | ⏳ Planned |
| Tracing Configuration | `OpenTelemetryTests.Tracing.cs` | 8 | 0 | ⏳ Planned |
| Metrics Configuration | `OpenTelemetryTests.Metrics.cs` | 8 | 0 | ⏳ Planned |
| Environment Variables | `OpenTelemetryTests.Environment.cs` | 8 | 0 | ⏳ Planned |
| Service Lifecycle | `MicroServiceTests.Startup.cs` | 11 | 0 | ⏳ Planned |
| Configuration Constants | `OpenTelemetryTests.Constants.cs` | 4 | 0 | ⏳ Planned |
| Pipeline Compatibility | `MicroServiceTests.PipelineModes.cs` | 7 | 0 | ⏳ Planned |
| Error Handling | `OpenTelemetryTests.ErrorHandling.cs` | 5 | 0 | ⏳ Planned |
| E2E Observability | `OpenTelemetryTests.E2E.cs` | 6 | 0 | 📋 Optional |
| Demo Validation | Demo project tests | 4 | 0 | 📋 Optional |

**Total:** 84 tests planned, 9 implemented (10.7% complete)

---

## Next Steps

1. Implement Resource Configuration Tests
2. Implement Logging Configuration Tests
3. Implement Tracing Configuration Tests
4. Implement Metrics Configuration Tests
5. Implement Environment Variable Tests
6. Implement Service Lifecycle Integration Tests
7. Implement Pipeline Mode Compatibility Tests
8. Implement Error Handling Tests
9. Consider implementing E2E Observability Tests
10. Consider implementing Demo Validation Tests
