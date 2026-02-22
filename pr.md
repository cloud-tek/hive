## Summary

Adds the **Hive.HTTP** extension -- a standardized HTTP client infrastructure for Hive microservices built on Refit, `IHttpClientFactory`, and `Microsoft.Extensions.Http.Resilience`. Includes a testing support library, a comprehensive test suite, and demo inter-service communication wired through Aspire with service discovery.

## Hive.HTTP Extension

### Registration API

HTTP clients are registered via `WithHttpClient<TApi>()` extension methods on `IMicroService` / `IMicroServiceCore`:

```csharp
var service = new MicroService("my-service")
    .WithHttpClient<IProductApi>(client => client
        .Internal()
        .WithBaseAddress("http://products-service")
        .WithAuthentication(auth => auth.BearerToken(() => GetTokenAsync()))
        .WithResilience(r => r.WithRetry(3).WithCircuitBreaker()));
```

All overloads support an optional custom client name for configuration key mapping.

### Tiered Configuration

Each client is configured through two complementary layers that are merged at startup:

- **IConfiguration** (`Hive:Http:{ClientName}`) -- base address, flavour, authentication type/credentials, resilience policies, socket handler tuning
- **Fluent API** (`HiveHttpClientBuilder`) -- programmatic overrides and runtime providers (e.g., bearer token delegates)

Fluent values override configuration values when both are present.

### Validation

All registrations are validated at startup via FluentValidation. Validation failures are surfaced through `IHostedStartupService` so they correctly set `Lifetime.StartupFailed` and are observable by health checks, rather than silently failing during host build.

### Authentication

Built-in support for two authentication modes:

- **Bearer Token** -- delegates token acquisition to a user-provided `Func<Task<string>>`
- **API Key** -- sends a configured key in a specified header

Authentication is applied as a `DelegatingHandler` in the HTTP pipeline.

### Resilience

Integrates with `Microsoft.Extensions.Http.Resilience` providing:

- Retry with configurable max attempts and backoff
- Circuit breaker with configurable failure ratio, sampling duration, and break duration
- Per-attempt timeout
- Standard resilience handler (pre-configured defaults)

Resilience events are instrumented via a custom `HttpClientMeter` for OpenTelemetry metrics.

### Telemetry

A `TelemetryHandler` (outermost in the handler pipeline) records request/response metrics including duration, status codes, and client name as dimensions, integrating with the existing Hive.OpenTelemetry infrastructure.

### Socket Handler Configuration

Configurable `SocketsHttpHandler` settings for connection pool management:

- `PooledConnectionLifetime` (default: `Timeout.InfiniteTimeSpan`)
- `PooledConnectionIdleTimeout` (default: 1 min)
- `MaxConnectionsPerServer` (default: `int.MaxValue`)

## Hive.HTTP.Testing

Support library for testing Hive.HTTP clients:

- `WithTestHandler<TApi>(handler)` -- injects a custom `HttpMessageHandler` for a specific client
- `WithMockResponse<TApi>(func)` -- provides a lambda-based mock for quick response stubbing
- `MockHttpMessageHandler` -- simple handler implementation backed by a `Func<HttpRequestMessage, HttpResponseMessage>`

## Test Suite

15 tests across two test classes:

**ExtensionRegistrationTests** (3 tests)
- Verifies singleton Extension registration across single and multiple `WithHttpClient` calls

**ConfigurationTests** (12 tests)
- Config-only: base address from config, missing base address fails startup, missing config section fails startup
- Fluent-only: base address from fluent API, missing base address fails startup
- Config + fluent merge: fluent overrides config, config provides base address while fluent adds auth
- Multiple clients: independent configuration with mixed config/fluent strategies
- Custom client name: config key override via `WithHttpClient<TApi>("CustomName")`
- Authentication: bearer token type without provider fails startup, API key from config applied correctly
- Resilience: config-driven retry policy correctly retries on transient failures

## Demo: Inter-Service Communication

### New: `Hive.MicroServices.Demo.ApiControllers.Client`

Client library encapsulating inter-service communication:

- `IWeatherForecastApi` -- Refit interface matching the ApiControllers endpoint
- `WeatherForecast` -- shared DTO record
- `Startup.WithWeatherForecastApiClient()` -- extension method encapsulating registration so consumers never touch the raw Hive.HTTP API

### Updated: `Hive.MicroServices.Demo.Api`

- Replaced inline weather forecast generation with a proxied call to `Demo.ApiControllers` via `IWeatherForecastApi`
- Uses `WithWeatherForecastApiClient()` from the client library
- Added Aspire service discovery (`Microsoft.Extensions.ServiceDiscovery`)

### Updated: `Hive.MicroServices.Demo.Aspire`

- Wired `Demo.Api` with a `WithReference()` to `Demo.ApiControllers` for service discovery
- Set `Hive__Http__IWeatherForecastApi__BaseAddress` environment variable override using Aspire resource names

### Updated: `Hive.MicroServices.Demo.ApiControllers`

- Added `WithOpenTelemetry()` to enable distributed trace propagation across service boundaries

## New Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Refit` | 10.0.1 | Type-safe REST client interfaces |
| `Refit.HttpClientFactory` | 10.0.1 | IHttpClientFactory integration for Refit |
| `Microsoft.Extensions.Http.Resilience` | 10.1.0 | Resilience policies (retry, circuit breaker) |
| `Microsoft.Extensions.ServiceDiscovery` | 10.1.0 | Aspire service discovery (demo only) |

## New Projects

| Project | Type | Location |
|---------|------|----------|
| `Hive.HTTP` | Package (src) | `hive.extensions/src/Hive.HTTP/` |
| `Hive.HTTP.Testing` | Package (src) | `hive.extensions/src/Hive.HTTP.Testing/` |
| `Hive.HTTP.Tests` | Tests | `hive.extensions/tests/Hive.HTTP.Tests/` |
| `Hive.MicroServices.Demo.ApiControllers.Client` | Demo | `hive.microservices/demo/Hive.MicroServices.Demo.ApiControllers.Client/` |

## Test Plan

- [x] `dotnet tool run cloudtek-build --target All` passes (all checks, compile, unit tests, integration tests, pack, publish)
- [ ] Run Aspire AppHost and verify distributed traces flow from `Demo.Api` through `Demo.ApiControllers` in the Aspire dashboard
- [ ] Verify `/weatherforecast` endpoint on `Demo.Api` returns data from `Demo.ApiControllers`
