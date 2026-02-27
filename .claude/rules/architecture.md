# architecture.md

This file provides guidance to Claude Code (claude.ai/code) about the Hive repository architecture.

## Repository Structure

The monorepo is organized into four main components:

1. **hive.core/** - Foundation layer
   - `Hive.Abstractions` - Core abstractions (IMicroServiceCore, IMicroService, MicroServiceExtension, configuration patterns)
   - `Hive.Testing` - Testing utilities and custom xUnit attributes

2. **hive.logging/** - Logging infrastructure
   - Currently transitioning from Serilog to OpenTelemetry
   - Includes AppInsights and LogzIo integrations

3. **hive.extensions/** - Feature extensions
   - `Hive.Messaging` - Messaging extension built on Wolverine (RabbitMQ transport)
   - `Hive.HealthChecks` - Application-level health checks with threshold-based readiness gating

4. **hive.microservices/** - Microservices framework
   - `Hive.MicroServices` - Core orchestration framework
   - `Hive.MicroServices.Api` - REST API support (minimal and controller-based)
   - `Hive.MicroServices.GraphQL` - GraphQL support via HotChocolate
   - `Hive.MicroServices.Grpc` - gRPC support (standard and code-first)
   - `Hive.MicroServices.Job` - Background job/worker support
   - `demo/` - Reference implementations

5. **hive.opentelemetry/** - OpenTelemetry integration
   - Logging, tracing, and metrics via OTLP protocol

## Interface Hierarchy

Hive uses a two-tier interface hierarchy to support different hosting models:

**IMicroServiceCore** - Framework-agnostic base for all Hive hosts
- Core properties: `Name`, `Id`, `Environment`, `ConfigurationRoot`, `EnvironmentVariables`, `Args`
- Extension system: `Extensions` list, `RegisterExtension<T>()`
- Lifecycle: `InitializeAsync()`, `StartAsync()`, `StopAsync()`
- Hosting context: `ExternalLogger`, `HostingMode`, `CancellationTokenSource`
- Use when: Building extensions that work across hosting models

**IMicroService : IMicroServiceCore** - ASP.NET microservices with Kubernetes support
- K8s probes: `IsReady`, `IsStarted`
- Lifecycle events: `Lifetime` (IMicroServiceLifetime)
- Pipeline modes: `PipelineMode` (Api, GraphQL, gRPC, Job, None)
- Execution: `RunAsync(IConfigurationRoot, string[]) → Task<int>`
- Use when: Building ASP.NET microservices or ASP.NET-specific middleware

**IFunctionHost : IMicroServiceCore** - Azure Functions integration
- Functions-specific configuration: `ConfigureServices()`, `ConfigureFunctions()`
- Functions execution: `RunAsync(CancellationToken) → Task`
- Use when: Building Azure Functions with Hive framework

## Extension Pattern

All framework features are implemented as extensions inheriting from `MicroServiceExtension<T>`. Extensions participate in the service lifecycle through:

- Constructor receives `IMicroServiceCore` instance (works with all hosting models)
- `ConfigureServices(IServiceCollection, IConfiguration)` - Service registration
- `Configure(IApplicationBuilder)` - Middleware pipeline (ASP.NET only)
- `ConfigureBeforeReadinessProbe(IApplicationBuilder)` - Pre-readiness probe middleware (ASP.NET only)
- `ConfigureEndpoints(IEndpointRouteBuilder)` - Endpoint registration (ASP.NET only)
- `ConfigureHealthChecks(IHealthChecksBuilder)` - Health check registration (ASP.NET only)

### Creating Extensions with Compile-Time Safety

All extensions must inherit from the generic base class `MicroServiceExtension<TExtension>` where `TExtension` is the extension type itself. This provides compile-time safety through C# 11's static abstract interface members:

```csharp
public class MyExtension : MicroServiceExtension<MyExtension>
{
    public MyExtension(IMicroServiceCore service) : base(service) { }

    public override IServiceCollection ConfigureServices(
        IServiceCollection services,
        IMicroServiceCore microservice)
    {
        // Add your services here
        services.AddSingleton<IMyService, MyService>();
        return services;
    }
}
```

**Key Points:**
- The generic type parameter must match the extension class name (Curiously Recurring Template Pattern)
- A default factory method (`Create`) is inherited from the base class
- Extensions without the correct generic pattern will not compile when used with `RegisterExtension<T>()`

### Cross-Extension Discovery: IActivitySourceProvider

Extensions that create `ActivitySource` instances (for distributed tracing) should implement `IActivitySourceProvider` from `Hive.Abstractions`. The `Hive.OpenTelemetry` extension auto-discovers all registered extensions implementing this interface and subscribes to their activity sources automatically.

```csharp
public class MyExtension : MicroServiceExtension<MyExtension>, IActivitySourceProvider
{
    public MyExtension(IMicroServiceCore service) : base(service) { }

    public IEnumerable<string> ActivitySourceNames => ["MyExtension"];
}
```

This means users don't need to manually wire `builder.AddSource(...)` — registering both `.WithOpenTelemetry()` and a provider extension (e.g., `.WithMessaging()`) is sufficient for traces to flow end-to-end.

### Extension Registration Examples

```csharp
// ASP.NET Microservice - Using custom extension methods (most common)
new MicroService("service-name")
    .WithOpenTelemetry(logging: builder => { }, tracing: builder => { })
    .ConfigureApiPipeline(app => { });

// ASP.NET Microservice - Using RegisterExtension with compile-time safety
new MicroService("service-name")
    .RegisterExtension<MyExtension>()  // Compile-time checked
    .ConfigureApiPipeline(app => { });

// Azure Functions
new FunctionHost("function-name")
    .WithOpenTelemetry(logging: builder => { }, tracing: builder => { })
    .ConfigureServices((services, config) => { });
```

**Compile-Time Safety:**
The generic base class ensures that only properly implemented extensions can be registered. Extensions that don't inherit from `MicroServiceExtension<TExtension>` will produce a compiler error when used with `RegisterExtension<T>()`.

## Pipeline Modes

The framework supports multiple service types through pipeline modes:

- `Api` - Minimal APIs with endpoint routing
- `ApiControllers` - Traditional controller-based APIs
- `GraphQL` - GraphQL APIs (HotChocolate)
- `Grpc` - gRPC services (standard protobuf-first)
- `Job` - Background worker services
- `None` - Basic service without HTTP

Each mode configures a specific middleware pipeline and service registrations.

## Configuration Patterns

The framework provides two-phase configuration:

**Pre-configuration** - Used when configuration is needed before `IServiceProvider` is built:
```csharp
services.PreConfigureValidatedOptions<TOptions>(configuration.GetSection("SectionName"));
```

**Post-configuration** - Standard options pattern after service provider is available:
```csharp
services.ConfigureValidatedOptions<TOptions>(configuration.GetSection("SectionName"));
```

Both support validation via DataAnnotations, FluentValidation, or custom delegates.

## Kubernetes Integration

Built-in middleware provides probe endpoints:

- `/startup` - Startup probe (K8s)
- `/readiness` - Readiness probe (K8s)
- `/liveness` - Liveness probe (K8s)

The `IMicroService` interface includes `IsReady` and `IsStarted` flags for lifecycle management.

## Dependency Graph

```
Hive.Abstractions (foundation)
    ├── Hive.Testing
    ├── Hive.Logging
    ├── Hive.OpenTelemetry
    ├── Hive.HealthChecks (→ Hive.MicroServices)
    ├── Hive.Messaging (→ Hive.MicroServices)
    └── Hive.MicroServices
            ├── Hive.MicroServices.Api
            ├── Hive.MicroServices.GraphQL
            ├── Hive.MicroServices.Grpc
            └── Hive.MicroServices.Job
```

All projects reference `Hive.Abstractions`. There are no circular dependencies.