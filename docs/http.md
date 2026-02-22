# Hive.HTTP Design Document

## Overview

Hive.HTTP is a NuGet package providing standardized, strongly-typed HTTP client support for Hive microservices. It wraps [Refit](https://github.com/reactiveui/refit) and `IHttpClientFactory` with proper connection lifecycle management, OpenTelemetry instrumentation, pluggable authentication, and Polly-based resilience.

Two client flavours are supported:
- **Internal** — service-to-service calls within the cluster (shorter timeouts, aggressive retries, service discovery-friendly)
- **External** — calls to third-party APIs outside the cluster (longer timeouts, conservative retries)

## Motivation

HTTP client usage in .NET is fraught with pitfalls: socket exhaustion from disposing `HttpClient`, DNS staleness from singleton clients, and memory pressure from buffering responses. This package applies the recommended "version 9" approach from [josef.codes](https://josef.codes/you-are-probably-still-using-httpclient-wrong-and-it-is-destabilizing-your-software/):

- `IHttpClientFactory` with handler pooling and rotation
- `SocketsHttpHandler` with `PooledConnectionLifetime` for DNS compliance
- Fully async pipeline with `System.Text.Json`
- `CancellationToken` propagation throughout

## Module Location

```
hive.extensions/
├── src/
│   └── Hive.HTTP/
│       ├── Hive.HTTP.csproj
│       ├── Extension.cs                        # MicroServiceExtension<Extension>
│       ├── Startup.cs                          # WithHttpClient<T>() fluent API (with and without builder lambda)
│       ├── HiveHttpClientBuilder.cs            # Per-client fluent builder
│       ├── HttpClientRegistration.cs           # Internal model for client config
│       ├── HttpClientFlavour.cs                # Enum: Internal, External
│       ├── Configuration/
│       │   ├── HttpClientOptions.cs            # Per-client options (address, flavour, resilience, auth)
│       │   ├── ResilienceOptions.cs            # Resilience sub-options
│       │   ├── CircuitBreakerOptions.cs        # Circuit breaker sub-options
│       │   ├── AuthenticationOptions.cs        # Authentication sub-options
│       │   ├── SocketsHandlerOptions.cs        # SocketsHttpHandler sub-options
│       │   └── HttpClientRegistrationValidator.cs  # FluentValidation for merged config
│       ├── Authentication/
│       │   ├── IAuthenticationProvider.cs      # Pluggable auth abstraction
│       │   ├── BearerTokenProvider.cs          # Bearer token auth
│       │   ├── ApiKeyProvider.cs               # API key auth
│       │   ├── AuthenticationBuilder.cs        # Auth fluent config
│       │   └── AuthenticationHandler.cs        # DelegatingHandler for auth
│       ├── Resilience/
│       │   └── ResilienceBuilder.cs            # Resilience fluent config + Polly telemetry hooks
│       └── Telemetry/
│           ├── TelemetryHandler.cs             # DelegatingHandler for OTel
│           └── HttpClientMeter.cs              # Custom OTel Meter + instruments
└── tests/
    └── Hive.HTTP.Tests/
        └── Hive.HTTP.Tests.csproj
```

## Fluent API

Overloads of `WithHttpClient<T>()`:

- `WithHttpClient<T>()` — parameterless; all settings come from `IConfiguration` (see [Configuration](#configuration)). This is the **default** registration form — the client is fully defined by configuration. The config key defaults to `typeof(T).Name` (e.g., `IProductApi`).
- `WithHttpClient<T>(Action<HiveHttpClientBuilder>)` — provides fluent overrides on top of IConfiguration. Use this only for settings that require code (bearer token factories, custom auth providers, custom handlers). `BaseAddress` must be provided via IConfiguration or `.WithBaseAddress()`.
- `WithHttpClient<T>(string clientName)` — parameterless with explicit name override. Uses `clientName` as the IConfiguration key instead of `typeof(T).Name`.
- `WithHttpClient<T>(string clientName, Action<HiveHttpClientBuilder>)` — explicit name with fluent overrides.

The `clientName` parameter controls both the IConfiguration lookup key (`Hive:Http:{clientName}`) and the `IHttpClientFactory` named client key. This decouples configuration from C# type names, allowing interface renames without breaking configuration.

### Internal (service-to-service)

```csharp
new MicroService("order-service")
    .WithHttpClient<IProductApi>(client => client
        .Internal()
        .WithBaseAddress("https://product-service")
        .WithAuthentication(auth => auth.BearerToken(
            sp => sp.GetRequiredService<ITokenService>().GetTokenAsync))
        .WithResilience(resilience => resilience
            .WithRetry(maxRetries: 3)
            .WithCircuitBreaker()))
    .ConfigureApiPipeline(app => { });
```

### External (third-party API)

```csharp
new MicroService("my-service")
    .WithHttpClient<IGitHubApi>(client => client
        .External()
        .WithBaseAddress("https://api.github.com")
        .WithAuthentication(auth => auth.ApiKey("Authorization", "token ghp_xxx"))
        .WithResilience(resilience => resilience
            .WithRetry(maxRetries: 5)
            .WithTimeout(TimeSpan.FromSeconds(30))))
    .ConfigureApiPipeline(app => { });
```

### Multiple clients on one service

```csharp
new MicroService("aggregator-service")
    .WithHttpClient<IProductApi>(client => client
        .Internal()
        .WithBaseAddress("https://product-service"))
    .WithHttpClient<IInventoryApi>(client => client
        .Internal()
        .WithBaseAddress("https://inventory-service"))
    .WithHttpClient<IPaymentGatewayApi>(client => client
        .External()
        .WithBaseAddress("https://api.stripe.com")
        .WithAuthentication(auth => auth.BearerToken(
            sp => sp.GetRequiredService<IStripeTokenProvider>().GetTokenAsync)))
    .ConfigureApiPipeline(app => { });
```

## Configuration

### Overview

HTTP clients use a **tiered configuration model** where `IConfiguration` is the primary source of truth:

1. **IConfiguration** (primary) — the authoritative source for client settings. Base addresses, flavours, resilience policies, static authentication, and socket handler tuning should all be defined here. This enables environment-specific overrides (`appsettings.Development.json`), secret injection (Key Vault, environment variables), and operational changes without recompilation.
2. **Fluent API** (optional overrides) — used only when configuration requires code that cannot be expressed in JSON: async token factories (`BearerToken`), custom `IAuthenticationProvider` implementations, custom `DelegatingHandler` registrations, and `RefitSettings` customisation.

When both tiers set the same property, the **fluent API wins** — this is intentional to allow developers to override operational defaults during development or testing. Properties not set by either tier use flavour-specific defaults (Internal / External).

### Configuration Section

```json
{
  "Hive": {
    "Http": {
      "IProductApi": {
        "BaseAddress": "https://product-service",
        "Flavour": "Internal",
        "Resilience": {
          "MaxRetries": 3,
          "CircuitBreaker": {
            "Enabled": true,
            "FailureRatio": 0.5,
            "SamplingDuration": "00:00:30",
            "MinimumThroughput": 10,
            "BreakDuration": "00:00:30"
          },
          "PerAttemptTimeout": "00:00:10"
        },
        "Authentication": {
          "Type": "ApiKey",
          "HeaderName": "X-Api-Key",
          "Value": "my-api-key"
        },
        "SocketsHandler": {
          "PooledConnectionLifetime": "00:02:00",
          "PooledConnectionIdleTimeout": "00:01:00",
          "MaxConnectionsPerServer": 100
        }
      },
      "IGitHubApi": {
        "BaseAddress": "https://api.github.com",
        "Flavour": "External",
        "Resilience": {
          "MaxRetries": 5,
          "PerAttemptTimeout": "00:00:30"
        }
      }
    }
  }
}
```

The client name key (e.g., `IProductApi`) defaults to `typeof(T).Name` but can be overridden via `WithHttpClient<T>(string clientName)` or `WithHttpClient<T>(string clientName, Action<HiveHttpClientBuilder>)`.

### Options Classes

```csharp
public class HttpClientOptions
{
    public const string SectionKey = "Hive:Http";

    // Not marked [Required] — validated post-merge (config + fluent API)
    // so either source can provide it
    public string? BaseAddress { get; set; }

    public HttpClientFlavour Flavour { get; set; } = HttpClientFlavour.Internal;

    public ResilienceOptions Resilience { get; set; } = new();
    public AuthenticationOptions? Authentication { get; set; }
    public SocketsHandlerOptions SocketsHandler { get; set; } = new();
}

public class ResilienceOptions
{
    public int? MaxRetries { get; set; }
    public TimeSpan? PerAttemptTimeout { get; set; }
    public CircuitBreakerOptions? CircuitBreaker { get; set; }
}

public class CircuitBreakerOptions
{
    public bool Enabled { get; set; }
    public double FailureRatio { get; set; } = 0.5;
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(30);
    public int MinimumThroughput { get; set; } = 10;
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);
}

public class AuthenticationOptions
{
    [Required]
    public string Type { get; set; } = default!;  // "ApiKey" or "BearerToken"
    public string? HeaderName { get; set; }
    public string? Value { get; set; }
}

public class SocketsHandlerOptions
{
    public TimeSpan PooledConnectionLifetime { get; set; } = Timeout.InfiniteTimeSpan;
    public TimeSpan PooledConnectionIdleTimeout { get; set; } = TimeSpan.FromMinutes(1);
    public int MaxConnectionsPerServer { get; set; } = int.MaxValue;
}
```

### Merged Configuration Validation

The merged result (IConfiguration + fluent API) is validated using FluentValidation before the client is registered with `IHttpClientFactory`. This follows the same pattern as CORS validation in `Hive.MicroServices`.

```csharp
public class HttpClientRegistrationValidator : AbstractValidator<HttpClientRegistration>
{
    public HttpClientRegistrationValidator()
    {
        RuleFor(r => r.ClientName)
            .NotEmpty();

        RuleFor(r => r.BaseAddress)
            .NotEmpty()
            .WithMessage(r =>
                $"BaseAddress is required for HTTP client '{r.ClientName}'. " +
                $"Provide it via IConfiguration (Hive:Http:{r.ClientName}:BaseAddress) " +
                $"or the fluent API (.WithBaseAddress()).");

        RuleFor(r => r.BaseAddress)
            .Must(addr => Uri.TryCreate(addr, UriKind.Absolute, out _))
            .When(r => !string.IsNullOrEmpty(r.BaseAddress))
            .WithMessage(r =>
                $"BaseAddress '{r.BaseAddress}' for HTTP client '{r.ClientName}' is not a valid absolute URI.");

        RuleFor(r => r)
            .Must(r => r.AuthenticationProviderFactory is not null)
            .When(r => r.AuthenticationType == "BearerToken")
            .WithMessage(r =>
                $"HTTP client '{r.ClientName}' has Authentication.Type 'BearerToken' in configuration " +
                $"but no fluent API .WithAuthentication(auth => auth.BearerToken(...)) was provided. " +
                $"Bearer token authentication requires an async factory delegate.");

        RuleFor(r => r)
            .Must(r => r.AuthenticationProviderFactory is not null)
            .When(r => r.AuthenticationType == "Custom")
            .WithMessage(r =>
                $"HTTP client '{r.ClientName}' has Authentication.Type 'Custom' in configuration " +
                $"but no fluent API .WithAuthentication(auth => auth.Custom(...)) was provided.");

        When(r => r.Resilience?.CircuitBreaker is { Enabled: true }, () =>
        {
            RuleFor(r => r.Resilience!.CircuitBreaker!.FailureRatio)
                .InclusiveBetween(0.0, 1.0);

            RuleFor(r => r.Resilience!.CircuitBreaker!.MinimumThroughput)
                .GreaterThan(0);

            RuleFor(r => r.Resilience!.CircuitBreaker!.BreakDuration)
                .GreaterThan(TimeSpan.Zero);
        });
    }
}
```

### Configuration Loading

The `Extension` loads per-client options by binding each child section of `Hive:Http` directly to `HttpClientOptions`. This matches the flat JSON schema where client names are direct children of the `Http` section. The merged result (IConfiguration + fluent API) is validated using `HttpClientRegistrationValidator` (FluentValidation).

```csharp
ConfigureActions.Add((services, configuration) =>
{
    // Phase 1: Bind per-client options from IConfiguration
    var httpSection = configuration.GetSection(HttpClientOptions.SectionKey);
    var configuredClients = new Dictionary<string, HttpClientOptions>();

    if (httpSection.Exists())
    {
        foreach (var child in httpSection.GetChildren())
        {
            var clientOptions = new HttpClientOptions();
            child.Bind(clientOptions);
            configuredClients[child.Key] = clientOptions;
        }
    }

    foreach (var registration in _registrations)
    {
        var clientName = registration.ClientName; // explicit name or typeof(T).Name

        // Phase 1: Apply IConfiguration values (if section exists for this client)
        if (configuredClients.TryGetValue(clientName, out var clientConfig))
        {
            registration.ApplyConfiguration(clientConfig);
        }

        // Phase 2: Apply fluent API overrides (always runs, overwrites config values)
        registration.ApplyFluentOverrides();

        // Phase 3: Validate merged result via FluentValidation
        var validator = new HttpClientRegistrationValidator();
        validator.ValidateAndThrow(registration);

        // Register with IHttpClientFactory
        RegisterClient(services, registration);
    }
});
```

### Merge Semantics

IConfiguration provides the baseline; fluent API overrides only where explicitly set.

| Property | IConfiguration (primary) | Fluent API (override) | Result |
|----------|------------------------|-----------------------|--------|
| BaseAddress | `"https://product-service"` | not set | `"https://product-service"` |
| BaseAddress | `"https://product-service"` | `.WithBaseAddress("https://other")` | `"https://other"` |
| Flavour | `"Internal"` | `.External()` | `External` |
| MaxRetries | `3` | `.WithResilience(r => r.WithRetry(5))` | `5` |
| Authentication | `ApiKey` config | `.WithAuthentication(auth => auth.BearerToken(...))` | `BearerToken` (full replace) |
| SocketsHandler | custom values | not set | custom values from config |

### Authentication via Configuration

Only **static** authentication types can be configured via `IConfiguration`:

| Type | IConfiguration | Fluent API |
|------|---------------|------------|
| `ApiKey` | Fully configurable (`HeaderName`, `Value`) | Fully configurable |
| `BearerToken` | Not supported (requires async factory delegate) | Required — use `.WithAuthentication(auth => auth.BearerToken(...))` |
| `Custom` | Not supported (requires `IAuthenticationProvider` factory) | Required — use `.WithAuthentication(auth => auth.Custom(...))` |

If `Authentication.Type` is set to `"BearerToken"` in configuration without a corresponding fluent API `.WithAuthentication()` call, a startup validation error is thrown.

**Security:** Authentication secrets (API keys, tokens) must come from secure configuration sources — Azure Key Vault, environment variables, or .NET User Secrets — not plain `appsettings.json` files. The `IConfiguration` abstraction supports all of these transparently; the JSON examples in this document use placeholder values for illustration only.

### Startup Validation

The merged result is validated via `HttpClientRegistrationValidator` (FluentValidation) after both configuration phases complete. If validation fails, the service throws a `ValidationException` at startup with a descriptive message:

```
ValidationException: BaseAddress is required for HTTP client 'IProductApi'.
Provide it via IConfiguration (Hive:Http:IProductApi:BaseAddress) or the fluent API (.WithBaseAddress()).
```

| Scenario | IConfiguration | Fluent API | Result |
|----------|---------------|------------|--------|
| Config-only | `BaseAddress` present | no lambda | Starts |
| Config-only | `BaseAddress` missing | no lambda | `ValidationException` |
| Config-only | section missing | no lambda | `ValidationException` |
| Fluent-only | section missing | `.WithBaseAddress(...)` | Starts |
| Both | `BaseAddress` present | `.WithBaseAddress(...)` | Starts (fluent wins) |
| Neither | section missing | no `.WithBaseAddress()` | `ValidationException` |

### Usage Examples

#### Config-only (no fluent API overrides)

```json
{
  "Hive": {
    "Http": {
      "IProductApi": {
        "BaseAddress": "https://product-service",
        "Flavour": "Internal",
        "Resilience": { "MaxRetries": 3 }
      }
    }
  }
}
```

```csharp
new MicroService("order-service")
    .WithHttpClient<IProductApi>()   // default form — fully defined by IConfiguration
    .ConfigureApiPipeline(app => { });
```

#### Config + fluent API override

```json
{
  "Hive": {
    "Http": {
      "IPaymentGatewayApi": {
        "BaseAddress": "https://api.stripe.com",
        "Flavour": "External",
        "Resilience": {
          "MaxRetries": 5,
          "PerAttemptTimeout": "00:00:30",
          "CircuitBreaker": { "Enabled": true }
        }
      }
    }
  }
}
```

```csharp
new MicroService("billing-service")
    .WithHttpClient<IPaymentGatewayApi>(client => client
        // Auth requires code — can't be expressed in JSON
        .WithAuthentication(auth => auth.BearerToken(
            sp => sp.GetRequiredService<IStripeTokenProvider>().GetTokenAsync)))
    .ConfigureApiPipeline(app => { });
```

IConfiguration defines the client (base address, flavour, resilience); the fluent API adds only what requires code (bearer token factory).

#### Environment-specific overrides

```json
// appsettings.json (base)
{
  "Hive": {
    "Http": {
      "IProductApi": {
        "BaseAddress": "https://product-service",
        "Flavour": "Internal"
      }
    }
  }
}

// appsettings.Development.json (override)
{
  "Hive": {
    "Http": {
      "IProductApi": {
        "BaseAddress": "https://localhost:5001"
      }
    }
  }
}
```

Standard ASP.NET configuration layering applies — `appsettings.Development.json` overrides `appsettings.json`, and the fluent API overrides both.

## Handler Pipeline

```
Request → TelemetryHandler → AuthenticationHandler → [Custom Handlers] → Polly Resilience Pipeline → SocketsHttpHandler → Network
```

- **TelemetryHandler** (outermost) — captures total duration including retries
- **AuthenticationHandler** — injects credentials before each attempt
- **Custom Handlers** — user-provided `DelegatingHandler` implementations
- **Polly Resilience Pipeline** — retry, circuit breaker, per-attempt timeout via `Microsoft.Extensions.Http.Resilience`. Hive-specific telemetry (retry counts, circuit breaker state transitions) is emitted via Polly v8 event hooks configured on the strategy options — not via a wrapping `DelegatingHandler`
- **SocketsHttpHandler** — connection pooling (defaults match framework: `PooledConnectionLifetime = Infinite`, `PooledConnectionIdleTimeout = 1 min`)

## Core Components

### Extension (MicroServiceExtension)

A single `Extension` instance collects all `WithHttpClient<T>()` registrations. During `ConfigureActions`, for each registration it:

1. Binds per-client `HttpClientOptions` from `IConfiguration` by iterating child sections of `Hive:Http`
2. Merges per-client `IConfiguration` values with fluent API overrides (fluent API wins)
3. Validates the merged result via `HttpClientRegistrationValidator` (FluentValidation)
4. Calls `services.AddRefitClient<TApi>(refitSettings)`
5. Chains `.ConfigureHttpClient(c => c.BaseAddress = ...)`
6. Chains `.UseSocketsHttpHandler(handler => { ... })` using merged `SocketsHandlerOptions`
7. Chains `.AddHttpMessageHandler<TelemetryHandler>()`
8. Chains `.AddHttpMessageHandler<AuthenticationHandler>()` (if auth configured via config or fluent API)
9. Chains any custom handlers
10. Chains resilience pipeline via `.AddResilienceHandler()` using merged resilience settings, with Polly v8 event hooks for Hive telemetry

### HiveHttpClientBuilder

Per-client fluent builder API:

| Method | Description |
|--------|-------------|
| `.Internal()` | Marks as internal, applies internal defaults |
| `.External()` | Marks as external, applies external defaults |
| `.WithBaseAddress(string)` | Sets base URI |
| `.WithAuthentication(Action<AuthenticationBuilder>)` | Configures auth |
| `.WithResilience(Action<ResilienceBuilder>)` | Configures Polly |
| `.WithHandler<THandler>()` | Adds custom DelegatingHandler |
| `.WithRefitSettings(RefitSettings)` | Override Refit serialization settings |

The client name is set at the `WithHttpClient<T>()` call site, not on the builder — see [Fluent API](#fluent-api) overloads.

### Authentication

**`IAuthenticationProvider`** — abstraction with `Task ApplyAsync(HttpRequestMessage, CancellationToken)`

Built-in providers:
- **`BearerTokenProvider`** — calls async token factory, sets `Authorization: Bearer {token}`
- **`ApiKeyProvider`** — sets header with static value

**`AuthenticationBuilder`** fluent API:
- `.BearerToken(Func<IServiceProvider, Func<CancellationToken, Task<string>>>)` — dynamic token
- `.ApiKey(string headerName, string value)` — static API key
- `.Custom(Func<IServiceProvider, IAuthenticationProvider>)` — user-provided

### Resilience

Leverages `Microsoft.Extensions.Http.Resilience` (Polly v8):

Hive-specific resilience telemetry is emitted via **Polly v8 event hooks** configured on the strategy options. A wrapping `DelegatingHandler` cannot observe individual retry attempts or circuit breaker transitions because these occur inside the Polly pipeline's `SendAsync()`. The hooks are wired during `AddResilienceHandler()`:

```csharp
.AddResilienceHandler(clientName, builder =>
{
    builder.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = registration.MaxRetries,
        OnRetry = args =>
        {
            retryCounter.Add(1, clientTag, serverTag);
            return default;
        }
    });
    builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        OnOpened = args => { circuitBreakerGauge.Record(1, clientTag); return default; },
        OnClosed = args => { circuitBreakerGauge.Record(0, clientTag); return default; },
        OnHalfOpened = args => { circuitBreakerGauge.Record(2, clientTag); return default; }
    });
    builder.AddTimeout(registration.PerAttemptTimeout);
});
```

**`ResilienceBuilder`** fluent API:
- `.WithRetry(int maxRetries)` — retry with exponential backoff and jitter. Retries on Polly's default retryable conditions: HTTP 408, 429, 500, 502, 503, 504, and network errors (`HttpRequestException`, `TimeoutRejectedException`)
- `.WithCircuitBreaker(...)` — circuit breaker
- `.WithTimeout(TimeSpan)` — **per-attempt** timeout applied to each individual request (including each retry attempt)
- `.UseStandardResilience()` — Microsoft defaults (total timeout 30s, per-attempt timeout 10s, 3 retries, circuit breaker at 10% failure ratio)

**Internal defaults:** shorter per-attempt timeouts, more aggressive retries
**External defaults:** longer per-attempt timeouts, more conservative retries

**Circuit breaker scope:** Circuit breaker state is **shared across all instances** of a named client. When a circuit breaker opens, all callers using that client fail fast — this is the intended behaviour to protect a struggling downstream service from further load. The circuit breaker state lives in the Polly resilience pipeline (managed via DI), not in the `SocketsHttpHandler`, so it survives handler rotation.

### OpenTelemetry Instrumentation

**Custom Meter:** `Hive.HTTP`

| Instrument | Type | Description |
|------------|------|-------------|
| `hive.http.client.request.duration` | Histogram\<double\> | Request duration in milliseconds |
| `hive.http.client.request.count` | Counter\<long\> | Total requests |
| `hive.http.client.request.errors` | Counter\<long\> | Failed requests (non-success status) |
| `hive.http.client.resilience.retries` | Counter\<long\> | Total retry attempts |
| `hive.http.client.resilience.circuit_breaker.state` | ObservableGauge\<int\> | Circuit breaker state (0=closed, 1=open, 2=half-open) |

**Tags on all instruments:**

| Tag | Source |
|-----|--------|
| `service.name` | `IMicroServiceCore.Name` |
| `http.request.method` | Request method (GET, POST, etc.) |
| `server.address` | Target host |
| `http.response.status_code` | Response status code |
| `client.name` | Refit interface name (e.g., `IProductApi`) |

## SocketsHttpHandler Configuration

All clients use `SocketsHttpHandler` as the primary message handler:

```csharp
new SocketsHttpHandler
{
    PooledConnectionLifetime = Timeout.InfiniteTimeSpan,
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
    MaxConnectionsPerServer = int.MaxValue
}
```

This ensures:
- Connections are recycled every 2 minutes → DNS changes are respected
- Idle connections are cleaned up after 1 minute → no resource waste
- No socket exhaustion — connections are pooled, not disposed per request

## NuGet Dependencies

| Package | Purpose |
|---------|---------|
| `Refit` | Interface-to-HTTP-client source generation |
| `Refit.HttpClientFactory` | `AddRefitClient<T>()` for IHttpClientFactory integration |
| `Microsoft.Extensions.Http.Resilience` | Polly-based resilience (already in repo) |
| `System.Diagnostics.DiagnosticSource` | OpenTelemetry Meter/Activity APIs |

## Dependency Graph

```
Hive.Abstractions (foundation)
    ├── ...existing...
    └── Hive.HTTP
            ├── Refit.HttpClientFactory
            ├── Microsoft.Extensions.Http.Resilience
            └── System.Diagnostics.DiagnosticSource
```

## Testing

Hive.HTTP must be fully testable using Hive's existing testing infrastructure: `ConfigureTestHost()` / `TestServer`, `E2ETestBase`, `TestPortProvider`, and in-memory OpenTelemetry exporters.

### The Problem

When Service A uses `WithHttpClient<IProductApi>()` to call Service B, and Service B runs on `TestServer` in tests, the Refit client's `SocketsHttpHandler` cannot reach the in-process `TestServer` — it would try to open a real network connection. We need a way to replace the primary handler with the `TestServer`'s handler so the call stays in-process.

### Testing Utilities (Hive.HTTP.Testing)

A companion package `Hive.HTTP.Testing` (in `hive.extensions/src/Hive.HTTP.Testing/`) provides test-specific extensions:

#### `WithTestHandler<TApi>()` — Wire Refit client to a TestServer

```csharp
// Service B (target) running on TestServer
var serviceB = new MicroService("product-service")
    .InTestClass<MyTests>()
    .ConfigureApiPipeline(app =>
        app.MapGet("/products", () => Results.Ok(new[] { new Product("Widget") })))
    .ConfigureTestHost();

await serviceB.InitializeAsync(config);
await serviceB.StartAsync();

var testServer = ((MicroService)serviceB).Host.GetTestServer();

// Service A (caller) — Refit client wired to Service B's TestServer handler
var serviceA = new MicroService("order-service")
    .InTestClass<MyTests>()
    .WithHttpClient<IProductApi>(client => client
        .Internal()
        .WithBaseAddress(testServer.BaseAddress.ToString()))
    .WithTestHandler<IProductApi>(testServer.CreateHandler())  // replaces SocketsHttpHandler
    .ConfigureApiPipeline(app => { })
    .ConfigureTestHost();
```

`WithTestHandler<TApi>(HttpMessageHandler)` overrides the primary handler for the named client matching `TApi`, so the full delegating handler pipeline (telemetry, auth, resilience) is preserved while the innermost handler routes to the TestServer.

#### `WithMockResponse<TApi>()` — Static mock responses without a real target

```csharp
var service = new MicroService("order-service")
    .InTestClass<MyTests>()
    .WithHttpClient<IProductApi>(client => client
        .Internal()
        .WithBaseAddress("https://product-service"))
    .WithMockResponse<IProductApi>(request =>
    {
        if (request.RequestUri?.PathAndQuery == "/products")
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[] { new Product("Widget") })
            };
        return new HttpResponseMessage(HttpStatusCode.NotFound);
    })
    .ConfigureApiPipeline(app => { })
    .ConfigureTestHost();
```

`WithMockResponse<TApi>()` replaces the primary handler with a `DelegatingHandler` that invokes the provided function, enabling fully in-memory tests without any target service.

### Test Scenarios

#### 1. Unit Tests — Extension registration and builder API

```csharp
[Fact]
[UnitTest]
public void GivenWithHttpClient_WhenCalled_ThenExtensionIsRegistered()
{
    var service = new MicroService("test-service", new NullLogger<IMicroService>());

    service.WithHttpClient<IProductApi>(client => client
        .Internal()
        .WithBaseAddress("https://product-service"));

    service.Extensions.Should().ContainSingle(e => e is Hive.HTTP.Extension);
}
```

#### 2. Configuration & Registration Tests

```csharp
[Fact]
[UnitTest]
public async Task GivenConfigOnly_WhenBaseAddressProvided_ThenClientIsRegistered()
{
    var config = new ConfigurationBuilder()
        .UseDefaultLoggingConfiguration()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Hive:Http:IProductApi:BaseAddress"] = "https://product-service",
            ["Hive:Http:IProductApi:Flavour"] = "Internal"
        })
        .Build();

    var service = new MicroService("test-service")
        .InTestClass<ConfigTests>()
        .WithHttpClient<IProductApi>()  // parameterless — all from config
        .WithMockResponse<IProductApi>(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<string>())
        })
        .ConfigureApiPipeline(app => { })
        .ConfigureTestHost();

    await service.ShouldStart(config);

    var provider = ((MicroService)service).Host.Services;
    var api = provider.GetRequiredService<IProductApi>();
    api.Should().NotBeNull();

    await service.StopAsync();
}

[Fact]
[UnitTest]
public async Task GivenConfigOnly_WhenBaseAddressMissing_ThenStartupFails()
{
    var config = new ConfigurationBuilder()
        .UseDefaultLoggingConfiguration()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Hive:Http:IProductApi:Flavour"] = "Internal"
            // BaseAddress intentionally omitted
        })
        .Build();

    var service = new MicroService("test-service")
        .InTestClass<ConfigTests>()
        .WithHttpClient<IProductApi>()
        .ConfigureApiPipeline(app => { })
        .ConfigureTestHost();

    await service.ShouldFailToStart(config);
}

[Fact]
[UnitTest]
public async Task GivenConfigOnly_WhenSectionMissing_ThenStartupFails()
{
    var config = new ConfigurationBuilder()
        .UseDefaultLoggingConfiguration()
        .Build(); // no Hive:Http section at all

    var service = new MicroService("test-service")
        .InTestClass<ConfigTests>()
        .WithHttpClient<IProductApi>()
        .ConfigureApiPipeline(app => { })
        .ConfigureTestHost();

    await service.ShouldFailToStart(config);
}

[Fact]
[UnitTest]
public async Task GivenFluentOnly_WhenBaseAddressProvided_ThenClientIsRegistered()
{
    var config = new ConfigurationBuilder()
        .UseDefaultLoggingConfiguration()
        .Build();

    var service = new MicroService("test-service")
        .InTestClass<ConfigTests>()
        .WithHttpClient<IProductApi>(client => client
            .Internal()
            .WithBaseAddress("https://product-service"))
        .WithMockResponse<IProductApi>(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<string>())
        })
        .ConfigureApiPipeline(app => { })
        .ConfigureTestHost();

    await service.ShouldStart(config);

    var provider = ((MicroService)service).Host.Services;
    var api = provider.GetRequiredService<IProductApi>();
    api.Should().NotBeNull();

    await service.StopAsync();
}

[Fact]
[UnitTest]
public async Task GivenFluentOnly_WhenBaseAddressMissing_ThenStartupFails()
{
    var config = new ConfigurationBuilder()
        .UseDefaultLoggingConfiguration()
        .Build();

    var service = new MicroService("test-service")
        .InTestClass<ConfigTests>()
        .WithHttpClient<IProductApi>(client => client.Internal())
        // no .WithBaseAddress() and no config
        .ConfigureApiPipeline(app => { })
        .ConfigureTestHost();

    await service.ShouldFailToStart(config);
}

[Fact]
[UnitTest]
public async Task GivenConfigAndFluent_WhenFluentOverridesBaseAddress_ThenFluentWins()
{
    HttpRequestMessage? capturedRequest = null;

    var config = new ConfigurationBuilder()
        .UseDefaultLoggingConfiguration()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Hive:Http:IProductApi:BaseAddress"] = "https://from-config",
            ["Hive:Http:IProductApi:Flavour"] = "Internal"
        })
        .Build();

    var service = new MicroService("test-service")
        .InTestClass<ConfigTests>()
        .WithHttpClient<IProductApi>(client => client
            .WithBaseAddress("https://from-fluent"))
        .WithMockResponse<IProductApi>(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Array.Empty<string>())
            };
        })
        .ConfigureApiPipeline(app => { })
        .ConfigureTestHost();

    await service.ShouldStart(config);

    var api = ((MicroService)service).Host.Services.GetRequiredService<IProductApi>();
    await api.GetProducts();

    capturedRequest!.RequestUri!.Host.Should().Be("from-fluent");

    await service.StopAsync();
}

[Fact]
[UnitTest]
public async Task GivenConfigAndFluent_WhenConfigProvidesBaseAddressAndFluentAddsAuth_ThenBothApplied()
{
    HttpRequestMessage? capturedRequest = null;

    var config = new ConfigurationBuilder()
        .UseDefaultLoggingConfiguration()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Hive:Http:IProductApi:BaseAddress"] = "https://product-service",
            ["Hive:Http:IProductApi:Flavour"] = "Internal",
            ["Hive:Http:IProductApi:Resilience:MaxRetries"] = "3"
        })
        .Build();

    var service = new MicroService("test-service")
        .InTestClass<ConfigTests>()
        .WithHttpClient<IProductApi>(client => client
            .WithAuthentication(auth => auth.BearerToken(
                _ => (_, _) => Task.FromResult("test-token"))))
        .WithMockResponse<IProductApi>(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Array.Empty<string>())
            };
        })
        .ConfigureApiPipeline(app => { })
        .ConfigureTestHost();

    await service.ShouldStart(config);

    var api = ((MicroService)service).Host.Services.GetRequiredService<IProductApi>();
    await api.GetProducts();

    // BaseAddress from config, auth from fluent — both applied
    capturedRequest!.RequestUri!.Host.Should().Be("product-service");
    capturedRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
    capturedRequest!.Headers.Authorization!.Parameter.Should().Be("test-token");

    await service.StopAsync();
}

[Fact]
[UnitTest]
public async Task GivenMultipleClients_WhenMixingConfigAndFluent_ThenEachClientConfiguredIndependently()
{
    var config = new ConfigurationBuilder()
        .UseDefaultLoggingConfiguration()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Hive:Http:IProductApi:BaseAddress"] = "https://product-service",
            ["Hive:Http:IProductApi:Flavour"] = "Internal"
        })
        .Build();

    var service = new MicroService("test-service")
        .InTestClass<ConfigTests>()
        .WithHttpClient<IProductApi>()  // config-only
        .WithHttpClient<IInventoryApi>(client => client
            .Internal()
            .WithBaseAddress("https://inventory-service"))  // fluent-only
        .WithMockResponse<IProductApi>(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<string>())
        })
        .WithMockResponse<IInventoryApi>(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<string>())
        })
        .ConfigureApiPipeline(app => { })
        .ConfigureTestHost();

    await service.ShouldStart(config);

    var provider = ((MicroService)service).Host.Services;
    provider.GetRequiredService<IProductApi>().Should().NotBeNull();
    provider.GetRequiredService<IInventoryApi>().Should().NotBeNull();

    await service.StopAsync();
}

[Fact]
[UnitTest]
public async Task GivenClientNameOverride_WhenConfigUsesCustomKey_ThenClientIsRegistered()
{
    var config = new ConfigurationBuilder()
        .UseDefaultLoggingConfiguration()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            // Config uses custom key "ProductService" instead of "IProductApi"
            ["Hive:Http:ProductService:BaseAddress"] = "https://product-service",
            ["Hive:Http:ProductService:Flavour"] = "Internal"
        })
        .Build();

    var service = new MicroService("test-service")
        .InTestClass<ConfigTests>()
        .WithHttpClient<IProductApi>("ProductService")  // clientName overrides config key
        .WithMockResponse<IProductApi>(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<string>())
        })
        .ConfigureApiPipeline(app => { })
        .ConfigureTestHost();

    await service.ShouldStart(config);

    var provider = ((MicroService)service).Host.Services;
    var api = provider.GetRequiredService<IProductApi>();
    api.Should().NotBeNull();

    await service.StopAsync();
}

[Fact]
[UnitTest]
public async Task GivenConfig_WhenBearerTokenTypeWithoutFluentAuth_ThenStartupFails()
{
    var config = new ConfigurationBuilder()
        .UseDefaultLoggingConfiguration()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Hive:Http:IProductApi:BaseAddress"] = "https://product-service",
            ["Hive:Http:IProductApi:Authentication:Type"] = "BearerToken"
            // No fluent API .WithAuthentication() — should fail
        })
        .Build();

    var service = new MicroService("test-service")
        .InTestClass<ConfigTests>()
        .WithHttpClient<IProductApi>()
        .ConfigureApiPipeline(app => { })
        .ConfigureTestHost();

    await service.ShouldFailToStart(config);
}

[Fact]
[UnitTest]
public async Task GivenConfig_WhenApiKeyAuthConfigured_ThenHeaderIsApplied()
{
    HttpRequestMessage? capturedRequest = null;

    var config = new ConfigurationBuilder()
        .UseDefaultLoggingConfiguration()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Hive:Http:IProductApi:BaseAddress"] = "https://product-service",
            ["Hive:Http:IProductApi:Authentication:Type"] = "ApiKey",
            ["Hive:Http:IProductApi:Authentication:HeaderName"] = "X-Api-Key",
            ["Hive:Http:IProductApi:Authentication:Value"] = "test-key-456"
        })
        .Build();

    var service = new MicroService("test-service")
        .InTestClass<ConfigTests>()
        .WithHttpClient<IProductApi>()
        .WithMockResponse<IProductApi>(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Array.Empty<string>())
            };
        })
        .ConfigureApiPipeline(app => { })
        .ConfigureTestHost();

    await service.ShouldStart(config);

    var api = ((MicroService)service).Host.Services.GetRequiredService<IProductApi>();
    await api.GetProducts();

    capturedRequest!.Headers.GetValues("X-Api-Key").Should().ContainSingle("test-key-456");

    await service.StopAsync();
}

[Fact]
[UnitTest]
public async Task GivenConfig_WhenResilienceConfigured_ThenResiliencePipelineIsActive()
{
    var callCount = 0;

    var config = new ConfigurationBuilder()
        .UseDefaultLoggingConfiguration()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Hive:Http:IProductApi:BaseAddress"] = "https://product-service",
            ["Hive:Http:IProductApi:Resilience:MaxRetries"] = "2"
        })
        .Build();

    var service = new MicroService("test-service")
        .InTestClass<ConfigTests>()
        .WithHttpClient<IProductApi>()
        .WithMockResponse<IProductApi>(_ =>
        {
            callCount++;
            // First two calls fail, third succeeds (after 2 retries)
            return callCount <= 2
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new[] { "Widget" })
                };
        })
        .ConfigureApiPipeline(app => { })
        .ConfigureTestHost();

    await service.ShouldStart(config);

    var api = ((MicroService)service).Host.Services.GetRequiredService<IProductApi>();
    var products = await api.GetProducts();

    products.Should().Contain("Widget");
    callCount.Should().Be(3); // 1 initial + 2 retries

    await service.StopAsync();
}
```

#### 3. Integration Tests — Full handler pipeline with TestServer

```csharp
[Fact]
[IntegrationTest]
public async Task GivenInternalClient_WhenCallingTargetService_ThenResponseIsReturned()
{
    var config = new ConfigurationBuilder().UseDefaultLoggingConfiguration().Build();

    // Start target service
    var target = new MicroService("product-service")
        .InTestClass<HttpClientTests>()
        .ConfigureApiPipeline(app =>
            app.MapGet("/products", () => Results.Ok(new[] { "Widget" })))
        .ConfigureTestHost();

    await target.InitializeAsync(config);
    await target.StartAsync();
    var testServer = ((MicroService)target).Host.GetTestServer();

    // Start caller service with Refit client pointing to target
    var caller = new MicroService("order-service")
        .InTestClass<HttpClientTests>()
        .WithHttpClient<IProductApi>(client => client
            .Internal()
            .WithBaseAddress(testServer.BaseAddress.ToString()))
        .WithTestHandler<IProductApi>(testServer.CreateHandler())
        .ConfigureApiPipeline(app => { })
        .ConfigureTestHost();

    await caller.InitializeAsync(config);
    await caller.StartAsync();

    // Resolve the Refit client from DI and call the target
    var provider = ((MicroService)caller).Host.Services;
    var productApi = provider.GetRequiredService<IProductApi>();
    var products = await productApi.GetProducts();

    products.Should().Contain("Widget");

    await caller.StopAsync();
    await target.StopAsync();
}
```

#### 3. E2E Telemetry Tests — Verify metrics and traces

Following the `E2ETestBase` pattern from `hive.opentelemetry`:

```csharp
[Fact]
[IntegrationTest]
[Collection("E2E Tests")]
public async Task GivenHttpClient_WhenRequestMade_ThenTelemetryIsEmitted()
{
    var service = CreateTestService<TelemetryTests>(
        "telemetry-test",
        app => app.MapGet("/invoke", async (IProductApi api) =>
        {
            var products = await api.GetProducts();
            return Results.Ok(products);
        }));

    // Wire Refit client to mock response
    service.WithMockResponse<IProductApi>(req =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new[] { "Widget" })
        });

    await RunServiceAndExecuteAsync(service, async () =>
    {
        using var client = CreateHttpClient();
        await client.GetAsync("/invoke");
        await Task.Delay(100);
    });

    // Verify Hive.HTTP meter emitted metrics
    ExportedMetrics.Should().Contain(m => m.Name == "hive.http.client.request.duration");
    ExportedMetrics.Should().Contain(m => m.Name == "hive.http.client.request.count");
}
```

#### 4. Authentication Tests — Verify auth headers are applied

```csharp
[Fact]
[IntegrationTest]
public async Task GivenBearerTokenAuth_WhenRequestMade_ThenAuthorizationHeaderIsSet()
{
    HttpRequestMessage? capturedRequest = null;

    var service = new MicroService("auth-test")
        .InTestClass<AuthTests>()
        .WithHttpClient<IProductApi>(client => client
            .Internal()
            .WithBaseAddress("https://product-service")
            .WithAuthentication(auth => auth.BearerToken(
                _ => (_, _) => Task.FromResult("test-token-123"))))
        .WithMockResponse<IProductApi>(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Array.Empty<string>())
            };
        })
        .ConfigureApiPipeline(app => { })
        .ConfigureTestHost();

    // ... start, resolve IProductApi, call, assert
    capturedRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
    capturedRequest!.Headers.Authorization!.Parameter.Should().Be("test-token-123");
}
```

### Module Layout (updated with testing)

```
hive.extensions/
├── src/
│   ├── Hive.HTTP/
│   │   └── ... (production code)
│   └── Hive.HTTP.Testing/
│       ├── Hive.HTTP.Testing.csproj
│       ├── IMicroServiceHttpTestExtensions.cs   # WithTestHandler<T>, WithMockResponse<T>
│       └── MockHttpMessageHandler.cs            # Handler that invokes response factory
└── tests/
    └── Hive.HTTP.Tests/
        └── Hive.HTTP.Tests.csproj
```

`Hive.HTTP.Testing` references both `Hive.HTTP` and `Microsoft.AspNetCore.TestHost`, following the same pattern as `Hive.MicroServices.Testing`.

## Future: gRPC Client Support

`IAuthenticationProvider` and the telemetry meter patterns are protocol-agnostic. When `Hive.Grpc` client support is added, authentication providers can be reused and `HttpClientMeter` can serve as a template for `GrpcClientMeter`.
