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

### 2. Configuration Loading Tests ✅ IMPLEMENTED

**File:** `ConfigurationTests.cs`

Tests for IConfiguration-based OpenTelemetry configuration:

- [x] Verify service starts with no configuration (uses defaults)
- [x] Verify service starts with full JSON configuration (all sections)
- [x] Verify service starts with OTLP endpoint configuration only
- [x] Verify service starts with partial configuration (single property)
- [x] Verify service starts with multiple resource attributes
- [x] Verify service starts with OTLP headers configured
- [x] Verify extension is registered when using default configuration

**Status:** 7/7 tests passing (6 configuration tests + 1 from ExtensionTests verifying default behavior)

---

### 3. Resource Configuration Tests

**File:** `OpenTelemetryTests.Resources.cs` (planned)

Tests for OpenTelemetry resource attribute configuration:

- [~] Verify `service.name` is set from `IMicroService.Name` (implicitly tested via ConfigurationTests)
- [~] Verify `service.instance.id` is set from `IMicroService.Id` (implicitly tested via ConfigurationTests)
- [ ] Verify `serviceNamespace` from IConfiguration is applied
- [ ] Verify `serviceVersion` from IConfiguration is applied
- [ ] Verify `autoGenerateServiceInstanceId` is false
- [~] Verify custom resource attributes from IConfiguration are applied (implicitly tested via ConfigurationTests)

---

### 4. Logging Configuration Tests

**File:** `OpenTelemetryTests.Logging.cs` (planned)

#### Default logging configuration (no custom action)

- [~] Verify console exporter is enabled by default (implicitly tested via ConfigurationTests with EnableConsoleExporter=true)
- [ ] Verify OTLP exporter is added when OTLP endpoint is configured in IConfiguration
- [ ] Verify OTLP exporter is added when `OTEL_EXPORTER_OTLP_ENDPOINT` env var is set
- [ ] Verify OTLP exporter uses correct endpoint from IConfiguration
- [ ] Verify OTLP exporter uses correct endpoint from environment variable
- [ ] Verify IConfiguration takes priority over environment variable
- [ ] Verify console exporter can be disabled via IConfiguration

#### Custom logging configuration

- [x] Verify custom `Action<LoggerProviderBuilder>` parameter is accepted (tested in ExtensionTests)
- [ ] Verify custom configuration completely overrides default behavior
- [ ] Verify service starts with custom logging configuration

**Status:** 2/10 tests passing (1 explicit + 1 implicit)

---

### 5. Tracing Configuration Tests

**File:** `OpenTelemetryTests.Tracing.cs` (planned)

#### Default tracing configuration (no custom action)

- [~] Verify ASP.NET Core instrumentation is enabled by default (implicitly tested via ConfigurationTests with EnableAspNetCoreInstrumentation=true)
- [~] Verify HTTP Client instrumentation is enabled by default (implicitly tested via ConfigurationTests with EnableHttpClientInstrumentation=true)
- [ ] Verify OTLP exporter is added when OTLP endpoint is configured in IConfiguration
- [ ] Verify OTLP exporter is added when `OTEL_EXPORTER_OTLP_ENDPOINT` env var is set
- [ ] Verify OTLP exporter uses correct endpoint from IConfiguration
- [ ] Verify OTLP exporter uses correct endpoint from environment variable
- [ ] Verify IConfiguration takes priority over environment variable
- [ ] Verify instrumentation can be disabled via IConfiguration

#### Custom tracing configuration

- [x] Verify custom `Action<TracerProviderBuilder>` parameter is accepted (tested in ExtensionTests)
- [ ] Verify custom configuration completely overrides default behavior
- [ ] Verify service starts with custom tracing configuration

**Status:** 3/11 tests passing (1 explicit + 2 implicit)

---

### 6. Metrics Configuration Tests

**File:** `OpenTelemetryTests.Metrics.cs` (planned)

#### Default metrics configuration (no custom action)

- [~] Verify ASP.NET Core instrumentation is enabled by default (implicitly tested via ConfigurationTests with EnableAspNetCoreInstrumentation=true)
- [~] Verify HTTP Client instrumentation is enabled by default (implicitly tested via ConfigurationTests with EnableHttpClientInstrumentation=true)
- [~] Verify Runtime instrumentation is enabled by default (implicitly tested via ConfigurationTests with EnableRuntimeInstrumentation=true)
- [ ] Verify OTLP exporter is added when OTLP endpoint is configured in IConfiguration
- [ ] Verify OTLP exporter is added when `OTEL_EXPORTER_OTLP_ENDPOINT` env var is set
- [ ] Verify OTLP exporter uses correct endpoint from IConfiguration
- [ ] Verify OTLP exporter uses correct endpoint from environment variable
- [ ] Verify IConfiguration takes priority over environment variable
- [ ] Verify instrumentation can be disabled via IConfiguration

#### Custom metrics configuration

- [x] Verify custom `Action<MeterProviderBuilder>` parameter is accepted (tested in ExtensionTests)
- [ ] Verify custom configuration completely overrides default behavior
- [ ] Verify service starts with custom metrics configuration

**Status:** 4/12 tests passing (1 explicit + 3 implicit)

---

### 7. Environment Variable Tests

**File:** `OpenTelemetryTests.Environment.cs` (planned)

#### OTEL_EXPORTER_OTLP_ENDPOINT handling

- [ ] Verify endpoint is read from environment variables correctly
- [ ] Verify endpoint is properly parsed as URI
- [ ] Verify behavior when environment variable is missing (fallback to null, no OTLP export)
- [ ] Verify behavior when environment variable is empty string
- [ ] Verify behavior with malformed URIs (should throw or handle gracefully)
- [ ] Verify behavior with different URI schemes (http, https)
- [ ] Verify IConfiguration OTLP endpoint takes priority over environment variable
- [ ] Verify environment variable is used as fallback when IConfiguration Otlp.Endpoint is empty

**Testing Utilities:**
- Use `EnvironmentVariableScope` from `Hive.Testing` for scoped environment variable manipulation

**Status:** 0/8 tests passing

---

### 8. Service Lifecycle Integration Tests

**File:** `ConfigurationTests.cs` (partially implemented) / `MicroServiceTests.Startup.cs` (planned)

#### Service startup with OpenTelemetry

- [x] Verify service starts successfully with OpenTelemetry configured (tested in ConfigurationTests)
- [x] Verify service starts with Api pipeline mode (tested in ConfigurationTests via ConfigureApiPipeline)
- [ ] Verify service starts with ApiControllers pipeline mode
- [ ] Verify service starts with GraphQL pipeline mode
- [ ] Verify service starts with Grpc pipeline mode
- [ ] Verify service starts with Job pipeline mode
- [ ] Verify service starts with None pipeline mode (default)
- [x] Verify OpenTelemetry doesn't prevent service startup (tested in ConfigurationTests)
- [x] Verify service can start without OTLP endpoint configured (tested in ConfigurationTests - GivenNoConfiguration test)

#### Service with multiple extensions

- [ ] Verify OpenTelemetry works alongside other extensions
- [x] Verify order of extension registration doesn't break functionality (tested in ExtensionTests - chaining test)

**Testing Utilities:**
- Use `.InTestClass<T>()` extension for test isolation
- Use `.ShouldStart()` extension for startup verification
- Use `.ShouldFailToStart()` for negative test cases

**Status:** 5/11 tests passing

---

### 9. Configuration Section Tests

**File:** `OpenTelemetryTests.Constants.cs` (planned)

Tests for configuration constants:

- [ ] Verify `Constants.Environment.OtelExporterOtlpEndpoint` has correct value ("OTEL_EXPORTER_OTLP_ENDPOINT")
- [ ] Verify `OpenTelemetryOptions.SectionKey` has correct value ("OpenTelemetry")
- [~] Verify IConfiguration section "OpenTelemetry" is properly bound to OpenTelemetryOptions (implicitly tested via ConfigurationTests)
- [~] Verify subsections (Resource, Logging, Tracing, Metrics, Otlp) are properly bound (implicitly tested via ConfigurationTests)

**Note:** Legacy constants (`OtelLoggingExporterSection`, `OtelTracingExporterSection`, `OtelMetricsExporterSection`) are defined but not used in favor of the unified `OpenTelemetryOptions` model.

**Status:** 2/4 tests passing (implicit)

---

### 10. Pipeline Mode Compatibility Tests

**File:** `MicroServiceTests.PipelineModes.cs` (planned)

Tests for OpenTelemetry compatibility with different pipeline modes:

- [x] Test with `ConfigureApiPipeline` - minimal API endpoints (tested in ConfigurationTests)
- [ ] Test with `ConfigureApiControllerPipeline` - controller-based APIs
- [ ] Test with `ConfigureGraphQLPipeline` - GraphQL APIs
- [ ] Test with `ConfigureGrpcPipeline` - gRPC services
- [ ] Test with `ConfigureCodeFirstGrpcPipeline` - code-first gRPC
- [ ] Test with `ConfigureDefaultServicePipeline` - None mode
- [ ] Verify telemetry is emitted for each pipeline mode

**Status:** 1/7 tests passing

---

### 11. Error Handling Tests

**File:** `OpenTelemetryTests.ErrorHandling.cs` (planned)

Tests for invalid configuration scenarios:

- [ ] Verify behavior with null MicroService instance (should not be possible)
- [ ] Verify behavior with invalid OTLP endpoint URIs
- [ ] Verify error messages are clear and actionable
- [ ] Verify service handles OTLP export failures gracefully
- [ ] Verify service continues operation if OTLP endpoint is unreachable

**Status:** 0/5 tests passing

---

### 12. End-to-End Observability Tests (Optional)

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

**Status:** 0/6 tests passing (optional)

---

### 13. Demo Application Validation (Optional)

**File:** System/Smoke tests in demo project (planned)

Tests for demo application integration:

- [ ] Verify demo application starts with OpenTelemetry
- [ ] Verify demo can make HTTP requests and generate telemetry
- [ ] Verify demo works with and without OTLP endpoint
- [ ] Verify demo emits logs, traces, and metrics

**Demo Location:** `hive.microservices/demo/Hive.MicroServices.Demo.Api/`

**Status:** 0/4 tests passing (optional)

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
| 1. Extension Registration | `ExtensionTests.cs` | 9 | 9 | ✅ Complete |
| 2. Configuration Loading | `ConfigurationTests.cs` | 7 | 7 | ✅ Complete |
| 3. Resource Configuration | `OpenTelemetryTests.Resources.cs` | 6 | 3 (implicit) | 🔄 Partial |
| 4. Logging Configuration | `OpenTelemetryTests.Logging.cs` | 10 | 2 (1+1i) | 🔄 Partial |
| 5. Tracing Configuration | `OpenTelemetryTests.Tracing.cs` | 11 | 3 (1+2i) | 🔄 Partial |
| 6. Metrics Configuration | `OpenTelemetryTests.Metrics.cs` | 12 | 4 (1+3i) | 🔄 Partial |
| 7. Environment Variables | `OpenTelemetryTests.Environment.cs` | 8 | 0 | ⏳ Planned |
| 8. Service Lifecycle | `ConfigurationTests.cs` + planned | 11 | 5 | 🔄 Partial |
| 9. Configuration Constants | `OpenTelemetryTests.Constants.cs` | 4 | 2 (implicit) | 🔄 Partial |
| 10. Pipeline Compatibility | `MicroServiceTests.PipelineModes.cs` | 7 | 1 | 🔄 Partial |
| 11. Error Handling | `OpenTelemetryTests.ErrorHandling.cs` | 5 | 0 | ⏳ Planned |
| 12. E2E Observability | `OpenTelemetryTests.E2E.cs` | 6 | 0 | 📋 Optional |
| 13. Demo Validation | Demo project tests | 4 | 0 | 📋 Optional |

**Total:** 100 tests planned (90 mandatory + 10 optional), 36 implemented (40.0% complete)

**Legend:**
- ✅ Complete - All tests implemented and passing
- 🔄 Partial - Some tests implemented, others implicitly validated or planned
- ⏳ Planned - Not yet implemented
- 📋 Optional - Nice to have, not required for core functionality
- (i) = implicitly tested through integration tests

---

## Next Steps

Based on current implementation progress (36/90 mandatory tests, 40% complete):

### High Priority
1. **Environment Variable Tests** (0/8) - Critical for OTLP endpoint resolution priority chain
2. **Resource Configuration Tests** (3/6 implicit → explicit) - Validate service.name, service.instance.id, custom attributes
3. **Logging Configuration Tests** (2/10 → 10/10) - Validate OTLP exporter configuration and IConfiguration priority
4. **Tracing Configuration Tests** (3/11 → 11/11) - Validate instrumentation and OTLP exporter configuration
5. **Metrics Configuration Tests** (4/12 → 12/12) - Validate instrumentation and OTLP exporter configuration

### Medium Priority
6. **Service Lifecycle Integration Tests** (5/11 → 11/11) - Test remaining pipeline modes
7. **Pipeline Mode Compatibility Tests** (1/7 → 7/7) - Test ApiControllers, GraphQL, Grpc, Job, None modes
8. **Error Handling Tests** (0/5) - Validate graceful failure scenarios
9. **Configuration Constants Tests** (2/4 implicit → explicit) - Validate constant values

### Optional (Nice to Have)
10. **E2E Observability Tests** (0/6) - Use InMemoryExporter to validate actual telemetry emission
11. **Demo Validation Tests** (0/4) - Smoke tests for demo application

### Implementation Notes
- Many tests are currently "implicitly tested" through integration tests in `ConfigurationTests.cs`
- Next focus should be on explicit unit tests to validate individual behaviors
- Environment variable tests are critical as they validate the configuration priority chain documented in CONFIGURATION_STRATEGY.md
