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

- [x] Verify extension is added to service.Extensions
- [x] Verify fluent API returns the IMicroService instance for chaining
- [x] Verify called with `additionalActivitySources` adds the extension
- [x] Verify called with no parameters adds the extension
- [x] Verify called multiple times adds multiple extensions
- [x] Verify chained with other extensions adds all

**Status:** 6/6 tests passing

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

### 3. Resource Configuration Tests ✅ IMPLEMENTED

**File:** `ResourceConfigurationTests.cs`

Tests for OpenTelemetry resource attribute configuration:

- [x] Verify `service.name` is set from `IMicroService.Name`
- [x] Verify `service.instance.id` is set from `IMicroService.Id`
- [x] Verify `serviceNamespace` from IConfiguration is applied
- [x] Verify `serviceVersion` from IConfiguration is applied
- [x] Verify custom resource attributes from IConfiguration are applied
- [x] Verify default resource options when no configuration provided

**Status:** 6/6 tests passing

**Implementation Notes:**
- `service.name` is always set from `IMicroService.Name`
- `service.instance.id` is always set from `IMicroService.Id`
- `serviceNamespace` and `serviceVersion` are optional and loaded from IConfiguration
- Custom attributes can be added via `OpenTelemetry:Resource:Attributes:*` configuration
- Tests verify service starts successfully with various resource configurations

---

### 4. Logging Configuration Tests ✅ IMPLEMENTED

**File:** `LoggingConfigurationTests.cs`

#### Default logging configuration (configuration-driven)

- [x] Verify console exporter is enabled by default
- [x] Verify console exporter can be explicitly enabled
- [x] Verify console exporter can be disabled via IConfiguration
- [x] Verify OTLP exporter is added when OTLP endpoint is configured in IConfiguration
- [x] Verify OTLP exporter with explicit EnableOtlpExporter flag
- [x] Verify OTLP with gRPC protocol
- [x] Verify OTLP with HttpProtobuf protocol
- [x] Verify OTLP with custom timeout
- [x] Verify OTLP with custom headers

#### Environment variable fallback

- [x] Verify OTLP endpoint is read from environment variable when no configuration
- [x] Verify IConfiguration takes priority over environment variable
- [x] Verify service works without any OTLP endpoint (console only)
- [x] Verify empty configuration endpoint falls back to environment variable

#### Combined configuration

- [x] Verify both console and OTLP can be enabled simultaneously
- [x] Verify console disabled with OTLP enabled
- [x] Verify full logging configuration with all settings

**Status:** 16/16 tests passing

---

### 5. Tracing Configuration Tests ✅ IMPLEMENTED

**File:** `TracingConfigurationTests.cs`

#### Default tracing configuration (configuration-driven)

- [x] Verify default instrumentations are enabled
- [x] Verify ASP.NET Core instrumentation can be explicitly enabled
- [x] Verify ASP.NET Core instrumentation can be disabled via IConfiguration
- [x] Verify HTTP Client instrumentation can be explicitly enabled
- [x] Verify HTTP Client instrumentation can be disabled via IConfiguration
- [x] Verify all instrumentations can be disabled simultaneously

#### OTLP exporter configuration

- [x] Verify OTLP exporter is added when endpoint is configured in IConfiguration
- [x] Verify OTLP exporter with explicit EnableOtlpExporter flag
- [x] Verify OTLP with gRPC protocol
- [x] Verify OTLP with HttpProtobuf protocol
- [x] Verify OTLP with custom timeout
- [x] Verify OTLP with custom headers

#### Environment variable fallback

- [x] Verify OTLP endpoint is read from environment variable when no configuration
- [x] Verify IConfiguration takes priority over environment variable
- [x] Verify service works without any OTLP endpoint
- [x] Verify empty configuration endpoint falls back to environment variable

#### Additional activity sources and additive SDK configuration

- [x] Verify `additionalActivitySources` are accepted (GivenAdditionalActivitySources_WhenServiceStarts_ThenServiceStartsSuccessfully)
- [x] Verify additive `ConfigureOpenTelemetryTracerProvider` adds a custom source without replacing default instrumentation (E2E/AdditiveProviderConfigurationTests)

Custom SDK configuration is additive and never overrides IConfiguration-driven defaults.

#### Combined configuration

- [x] Verify all instrumentations and OTLP enabled simultaneously
- [x] Verify only ASP.NET Core instrumentation enabled
- [x] Verify only HTTP Client instrumentation enabled
- [x] Verify full tracing configuration with all settings
- [x] Verify tracing with resource attributes

**Status:** 22/22 tests passing

---

### 6. Metrics Configuration Tests ✅ IMPLEMENTED

**File:** `MetricsConfigurationTests.cs`

#### Default metrics configuration (configuration-driven)

- [x] Verify default instrumentations are enabled
- [x] Verify ASP.NET Core instrumentation can be explicitly enabled
- [x] Verify ASP.NET Core instrumentation can be disabled via IConfiguration
- [x] Verify HTTP Client instrumentation can be explicitly enabled
- [x] Verify HTTP Client instrumentation can be disabled via IConfiguration
- [x] Verify Runtime instrumentation can be explicitly enabled
- [x] Verify Runtime instrumentation can be disabled via IConfiguration
- [x] Verify all instrumentations can be disabled simultaneously

#### OTLP exporter configuration

- [x] Verify OTLP exporter is added when endpoint is configured in IConfiguration
- [x] Verify OTLP exporter with explicit EnableOtlpExporter flag
- [x] Verify OTLP with gRPC protocol
- [x] Verify OTLP with HttpProtobuf protocol
- [x] Verify OTLP with custom timeout
- [x] Verify OTLP with custom headers

#### Environment variable fallback

- [x] Verify OTLP endpoint is read from environment variable when no configuration
- [x] Verify IConfiguration takes priority over environment variable
- [x] Verify service works without any OTLP endpoint
- [x] Verify empty configuration endpoint falls back to environment variable

Additive `ConfigureOpenTelemetryMeterProvider` customisation is supported by the SDK but is not covered by a test.

#### Combined configuration

- [x] Verify all instrumentations and OTLP enabled simultaneously
- [x] Verify only ASP.NET Core instrumentation enabled
- [x] Verify only HTTP Client instrumentation enabled
- [x] Verify only Runtime instrumentation enabled
- [x] Verify full metrics configuration with all settings
- [x] Verify metrics with resource attributes
- [x] Verify two instrumentations enabled (ASP.NET Core + HTTP Client)

**Status:** 25/25 tests passing

---

### 7. Environment Variable Tests ✅ IMPLEMENTED

**File:** `LoggingConfigurationTests.cs`, `TracingConfigurationTests.cs`, `MetricsConfigurationTests.cs`

#### OTEL_EXPORTER_OTLP_ENDPOINT handling

- [x] Verify endpoint is read from environment variables correctly (Logging, Tracing, Metrics)
- [x] Verify behavior when environment variable is missing - no OTLP export (Logging, Tracing, Metrics)
- [x] Verify IConfiguration OTLP endpoint takes priority over environment variable (Logging, Tracing, Metrics)
- [x] Verify environment variable is used as fallback when IConfiguration Otlp.Endpoint is empty (Logging, Tracing, Metrics)

**Testing Utilities:**
- Uses `EnvironmentVariableScope` from `Hive.Testing` for scoped environment variable manipulation

**Status:** 12/12 tests passing (4 per signal type)

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

### 9. Configuration Section Tests ✅ IMPLEMENTED

**File:** `ConfigurationSectionTests.cs`

#### Constants value tests

- [x] Verify `Constants.Environment.OtelExporterOtlpEndpoint` has correct value
- [x] Verify `Constants.OtelLoggingExporterSection` has correct value
- [x] Verify `Constants.OtelTracingExporterSection` has correct value
- [x] Verify `Constants.OtelMetricsExporterSection` has correct value
- [x] Verify `OpenTelemetryOptions.SectionKey` has correct value

#### Options default value tests

- [x] Verify `OpenTelemetryOptions` has default Resource options
- [x] Verify `OpenTelemetryOptions` has default Logging options
- [x] Verify `OpenTelemetryOptions` has default Tracing options
- [x] Verify `OpenTelemetryOptions` has default Metrics options
- [x] Verify `OpenTelemetryOptions` has default OTLP options
- [x] Verify `ResourceOptions` has correct defaults
- [x] Verify `LoggingOptions` has correct defaults
- [x] Verify `TracingOptions` has correct defaults
- [x] Verify `MetricsOptions` has correct defaults
- [x] Verify `OtlpOptions` has correct defaults

#### Configuration binding tests

- [x] Verify section key is correct for binding
- [x] Verify Resource section binds correctly
- [x] Verify Logging section binds correctly
- [x] Verify Tracing section binds correctly
- [x] Verify Metrics section binds correctly
- [x] Verify OTLP section binds correctly
- [x] Verify full configuration binds correctly
- [x] Verify empty configuration preserves defaults
- [x] Verify partial configuration only changes specified values

#### Legacy section constants tests

- [x] Verify Logging section constant can access configuration
- [x] Verify Tracing section constant can access configuration
- [x] Verify Metrics section constant can access configuration

#### Protocol enum binding tests

- [x] Verify Grpc protocol binds correctly
- [x] Verify HttpProtobuf protocol binds correctly

**Status:** 29/29 tests passing

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

### 12. End-to-End Observability Tests ✅ IMPLEMENTED

**File:** `E2E/TraceEmissionTests.cs`, `E2E/MetricsEmissionTests.cs`, `E2E/LogEmissionTests.cs`, `E2E/ResourceCorrelationTests.cs`, `E2E/ContextPropagationTests.cs`, `E2E/AdditiveProviderConfigurationTests.cs`

Integration tests for actual telemetry emission using OpenTelemetry's InMemoryExporter:

#### Trace Emission Tests (6 tests)
- [x] Verify traces are created for HTTP requests
- [x] Verify multiple requests generate multiple traces
- [x] Verify HTTP method/status attributes are captured
- [x] Verify POST requests are traced correctly
- [x] Verify error status codes are captured
- [x] Verify traces have valid TraceIds

#### Metrics Emission Tests (6 tests)
- [x] Verify metrics are collected for HTTP requests
- [x] Verify metrics accumulate for multiple requests
- [x] Verify runtime instrumentation is active
- [x] Verify HTTP client instrumentation is active
- [x] Verify ASP.NET Core instrumentation distinguishes routes
- [x] Verify metrics service lifecycle completes without exceptions

#### Log Emission Tests (6 tests)
- [x] Verify logs are captured via OpenTelemetry
- [x] Verify different log levels are captured (Information, Warning, Error)
- [x] Verify structured logging state is captured
- [x] Verify exceptions are captured in log records
- [x] Verify logger category names are correct
- [x] Verify multiple logs are captured

#### Resource Correlation Tests (4 tests)
- [x] Verify traces contain service name
- [x] Verify logs contain category name
- [x] Verify configured resource attributes are applied
- [x] Verify service instance ID is consistent

#### Additive Provider Configuration Tests (1 test)
- [x] Verify additive `ConfigureOpenTelemetryTracerProvider` exports a custom activity source

#### Context Propagation Tests (4 tests)
- [x] Verify trace context is present on logs (TraceId correlation)
- [x] Verify nested activities share the same TraceId in logs
- [x] Verify each request has a unique TraceId
- [x] Verify incoming traceparent header is propagated

**Implementation Notes:**
- Uses `InMemoryExporter` for logs, traces, and metrics from `OpenTelemetry.Exporter.InMemory` package
- Uses xUnit collection with `DisableParallelization = true` to prevent port conflicts
- Uses static port counter with `Interlocked.Add` for unique port allocation per test
- Configures `OpenTelemetryLoggerOptions.IncludeFormattedMessage = true` for log assertion
- E2ETestBase provides helper methods for common test setup

**Status:** 27/27 tests passing

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
| 1. Extension Registration | `ExtensionTests.cs` | 6 | 6 | ✅ Complete |
| 2. Configuration Loading | `ConfigurationTests.cs` | 7 | 7 | ✅ Complete |
| 3. Resource Configuration | `ResourceConfigurationTests.cs` | 6 | 6 | ✅ Complete |
| 4. Logging Configuration | `LoggingConfigurationTests.cs` | 16 | 16 | ✅ Complete |
| 5. Tracing Configuration | `TracingConfigurationTests.cs` | 22 | 22 | ✅ Complete |
| 6. Metrics Configuration | `MetricsConfigurationTests.cs` | 25 | 25 | ✅ Complete |
| 7. Environment Variables | `*ConfigurationTests.cs` | 12 | 12 | ✅ Complete |
| 8. Service Lifecycle | `ConfigurationTests.cs` + planned | 11 | 5 | 🔄 Partial |
| 9. Configuration Sections | `ConfigurationSectionTests.cs` | 29 | 29 | ✅ Complete |
| 10. Pipeline Compatibility | `MicroServiceTests.PipelineModes.cs` | 7 | 1 | 🔄 Partial |
| 11. Error Handling | `OpenTelemetryTests.ErrorHandling.cs` | 5 | 0 | ⏳ Planned |
| 12. E2E Observability | `E2E/*.cs` | 27 | 27 | ✅ Complete |
| 13. Demo Validation | Demo project tests | 4 | 0 | 📋 Optional |

**Total:** 177 tests planned (173 mandatory + 4 optional), 156 implemented (90.2% of mandatory)

**Explicit Tests Passing:** 137 (6 Extension + 6 Configuration + 6 Resource + 16 Logging + 22 Tracing + 25 Metrics + 29 ConfigSection + 27 E2E)

**Legend:**
- ✅ Complete - All tests implemented and passing
- 🔄 Partial - Some tests implemented, others implicitly validated or planned
- ⏳ Planned - Not yet implemented
- 📋 Optional - Nice to have, not required for core functionality
- (i) = implicitly tested through integration tests

---

## Next Steps

Based on current implementation progress (156/173 mandatory tests, 90.2% complete):

### Medium Priority
1. **Service Lifecycle Integration Tests** (5/11 → 11/11) - Test remaining pipeline modes
2. **Pipeline Mode Compatibility Tests** (1/7 → 7/7) - Test ApiControllers, GraphQL, Grpc, Job, None modes
3. **Error Handling Tests** (0/5) - Validate graceful failure scenarios

### Optional (Nice to Have)
4. **Demo Validation Tests** (0/4) - Smoke tests for demo application

### Recently Completed ✅
- **E2E Observability Tests** (27/27) - Complete telemetry emission validation using InMemoryExporter
  - TraceEmissionTests (6 tests) - HTTP request tracing, attributes, error status
  - MetricsEmissionTests (6 tests) - HTTP metrics, runtime instrumentation
  - LogEmissionTests (6 tests) - Log capture, levels, structured logging, exceptions
  - ResourceCorrelationTests (4 tests) - Service name, category, resource attributes
  - ContextPropagationTests (4 tests) - TraceId correlation, nested activities, traceparent propagation
  - AdditiveProviderConfigurationTests (1 test) - additive tracer provider configuration
- **Configuration Section Tests** (29/29) - Constants, option defaults, configuration binding, protocol enum binding
- **Metrics Configuration Tests** (25/25) - All 3 instrumentations, OTLP exporter, environment variable fallback
- **Tracing Configuration Tests** (22/22) - Instrumentations, OTLP exporter, environment variable fallback, additional activity sources
- **Logging Configuration Tests** (16/16) - Console exporter, OTLP exporter, environment variable fallback
- **Environment Variable Tests** (12/12) - Covered within Logging, Tracing, and Metrics ConfigurationTests
- **Resource Configuration Tests** (6/6) - service.name, service.instance.id, serviceNamespace, serviceVersion, custom attributes

### Implementation Notes
- All three signal configuration tests (Logging, Tracing, Metrics) now provide comprehensive coverage
- Configuration section tests validate all constants, defaults, and binding behavior
- Environment variable priority chain tests are complete for all signal types
- E2E tests use OpenTelemetry's InMemoryExporter to validate actual telemetry emission
- E2E tests require `OpenTelemetryLoggerOptions.IncludeFormattedMessage = true` for log assertions
- Core OpenTelemetry functionality is fully tested; remaining tests are for edge cases and advanced scenarios
