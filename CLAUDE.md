# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Hive is a .NET 10.0 monorepo providing an opinionated, extensible microservices framework built on ASP.NET Core. It follows a plugin-based architecture where all features are implemented as extensions to the core `IMicroService` abstraction.

## C# 14 Features in Use
Hive uses .NET 10 with C# 14. The following features are valid:
- Extension members using `extension<T>(Type receiver)` syntax (NOT experimental)
- The `field` keyword in property accessors
- Implicit span conversions

## Common Commands

### Building

#### Using CloudTek.Build.Tool

The project is built using `CloudTek.Build.Tool` dotnet tool.

If missing, the tool can be installed using: `dotnet tool install CloudTek.Build.Tool`

```bash
# Run all targets to perform a complete build
dotnet tool run cloudtek-build --target All

# Run all targets, except for checks to perform a quick build
dotnet tool run cloudtek-build --target All --Skip RunChecks
```

#### Using dotnet cli
```bash
# Build the entire solution
dotnet build Hive.sln

# Build a specific project
dotnet build <project-path>/<project>.csproj

# Build with specific configuration
dotnet build -c Release
```

### Testing
```bash
# Run all tests
dotnet test Hive.sln

# Run tests for a specific project
dotnet test <test-project-path>/<test-project>.csproj

# Run tests with specific filter (by category)
dotnet test --filter Category=UnitTests
dotnet test --filter Category=IntegrationTests

# Run a single test
dotnet test --filter FullyQualifiedName~<TestClassName>.<TestMethodName>
```

### Running Demo Applications
```bash
# Run the API demo
dotnet run --project hive.microservices/demo/Hive.MicroServices.Demo.Api

# Run the Aspire orchestration (includes all demos)
dotnet run --project hive.microservices/demo/Hive.MicroServices.Demo.Aspire
```

## Architecture

### Repository Structure

The monorepo is organized into four main components:

1. **hive.core/** - Foundation layer
   - `Hive.Abstractions` - Core abstractions (IMicroServiceCore, IMicroService, MicroServiceExtension, configuration patterns)
   - `Hive.Testing` - Testing utilities and custom xUnit attributes

2. **hive.logging/** - Logging infrastructure
   - Currently transitioning from Serilog to OpenTelemetry
   - Includes AppInsights and LogzIo integrations

3. **hive.microservices/** - Microservices framework
   - `Hive.MicroServices` - Core orchestration framework
   - `Hive.MicroServices.Api` - REST API support (minimal and controller-based)
   - `Hive.MicroServices.GraphQL` - GraphQL support via HotChocolate
   - `Hive.MicroServices.Grpc` - gRPC support (standard and code-first)
   - `Hive.MicroServices.Job` - Background job/worker support
   - `demo/` - Reference implementations

4. **Hive.OpenTelemetry/** - OpenTelemetry integration (current development focus)
   - Logging, tracing, and metrics via OTLP protocol

### Interface Hierarchy

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

### Extension Pattern

All framework features are implemented as extensions inheriting from `MicroServiceExtension<T>`. Extensions participate in the service lifecycle through:

- Constructor receives `IMicroServiceCore` instance (works with all hosting models)
- `ConfigureServices(IServiceCollection, IConfiguration)` - Service registration
- `Configure(IApplicationBuilder)` - Middleware pipeline (ASP.NET only)
- `ConfigureBeforeReadinessProbe(IApplicationBuilder)` - Pre-readiness probe middleware (ASP.NET only)
- `ConfigureEndpoints(IEndpointRouteBuilder)` - Endpoint registration (ASP.NET only)
- `ConfigureHealthChecks(IHealthChecksBuilder)` - Health check registration (ASP.NET only)

#### Creating Extensions with Compile-Time Safety

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

#### Extension Registration Examples

```csharp
// ASP.NET Microservice - Using custom extension methods (most common)
new MicroService("service-name")
    .WithOpenTelemetry(logging: builder => { }, tracing: builder => { })
    .ConfigureApiPipeline(app => { });

// ASP.NET Microservice - Using RegisterExtension with compile-time safety
new MicroService("service-name")
    .RegisterExtension<MyExtension>()  // ✅ Compile-time checked
    .ConfigureApiPipeline(app => { });

// Azure Functions
new FunctionHost("function-name")
    .WithOpenTelemetry(logging: builder => { }, tracing: builder => { })
    .ConfigureServices((services, config) => { });
```

**Compile-Time Safety:**
The generic base class ensures that only properly implemented extensions can be registered. Extensions that don't inherit from `MicroServiceExtension<TExtension>` will produce a compiler error when used with `RegisterExtension<T>()`.

### Pipeline Modes

The framework supports multiple service types through pipeline modes:

- `Api` - Minimal APIs with endpoint routing
- `ApiControllers` - Traditional controller-based APIs
- `GraphQL` - GraphQL APIs (HotChocolate)
- `Grpc` - gRPC services (standard protobuf-first)
- `Job` - Background worker services
- `None` - Basic service without HTTP

Each mode configures a specific middleware pipeline and service registrations.

### Configuration Patterns

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

### Kubernetes Integration

Built-in middleware provides probe endpoints:

- `/startup` - Startup probe (K8s)
- `/readiness` - Readiness probe (K8s)
- `/liveness` - Liveness probe (K8s)

The `IMicroService` interface includes `IsReady` and `IsStarted` flags for lifecycle management.

## Package Management

This repository uses centralized package management (`ManagePackageVersionsCentrally=true`):

- All package versions are defined in `Directory.Packages.props`
- Projects reference packages WITHOUT version numbers
- `Directory.Build.props` sets global properties (target framework: net10.0)
- Custom MSBuild SDK: `CloudTek.Sdk` (version 10.0.0-beta.5)

When adding a new package:
1. Add the `<PackageVersion>` entry to `Directory.Packages.props`
2. Reference in project with `<PackageReference Include="PackageName" />` (no Version attribute)

## Testing Practices

### Test Attributes

Use custom xUnit trait attributes for test categorization:

- `[UnitTest]` - Unit tests (Category: "UnitTests")
- `[IntegrationTest]` - Integration tests (Category: "IntegrationTests")
- `[ModuleTest]` - Module tests (Category: "ModuleTests")
- `[SmokeTest]` - Smoke tests (Category: "SmokeTests")
- `[SystemTest]` - System tests (Category: "SystemTests")

### Testing Utilities

Available via `Hive.Testing`:

- `TestPortProvider` - Dynamic port allocation for integration tests
- `EnvironmentVariableScope` - Scoped environment variable manipulation
- `MicroServiceTestExtensions` - Extensions like `.InTestClass<T>()`, `.ShouldStart()`, `.ShouldFailToStart()`

Example:
```csharp
[Fact]
[UnitTest]
public async Task TestMicroServiceStartup()
{
    var service = new MicroService("test-service")
        .InTestClass<MyTestClass>()
        .ConfigureApiPipeline(app => { });

    service.CancellationTokenSource.CancelAfter(1000);
    await service.RunAsync(config);

    service.PipelineMode.Should().Be(MicroServicePipelineMode.Api);
}
```

## OpenTelemetry Integration (Current Work)

The `Hive.OpenTelemetry` extension provides unified observability:

```csharp
var service = new MicroService("service-name")
    .WithOpenTelemetry(
        logging: builder => { /* customize logging */ },
        tracing: builder => { /* customize tracing */ },
        metrics: builder => { /* customize metrics */ }
    );
```

**Environment Variables:**
- `OTEL_EXPORTER_OTLP_ENDPOINT` - OTLP collector endpoint (e.g., `http://localhost:4317`)

When the endpoint is set, telemetry is exported via OTLP; otherwise, console export is used.

**Resource Attributes:**
- `service.name` - From `IMicroService.Name`
- `service.instance.id` - From `IMicroService.Id`

**Instrumentation Includes:**
- ASP.NET Core (requests, exceptions)
- HTTP client (outbound calls)
- Runtime metrics (GC, thread pool, etc.)

## File References

When working with code, reference files using this format:

- Files: `Hive.Abstractions/IMicroService.cs`
- Specific lines: `Hive.MicroServices/MicroService.cs:42`
- Ranges: `Hive.MicroServices.Api/IMicroServiceExtensions.cs:10-25`

## Important Paths

- Core abstractions: `hive.core/src/Hive.Abstractions/`
- Main framework: `hive.microservices/src/Hive.MicroServices/`
- OpenTelemetry extension: `Hive.OpenTelemetry/`
- Demo applications: `hive.microservices/demo/`
- Test projects: `*/tests/`

## Current Development

**Branch:** `feature/hive.opentelemetry`

**Focus:** Implementing OpenTelemetry support for logs, traces, and metrics with OTLP protocol export. This represents a migration from the previous Serilog-based logging approach.

**Modified files:**
- `Hive.OpenTelemetry/Extension.cs` - Main extension implementation
- `Hive.OpenTelemetry/Startup.cs` - Startup configuration
- `Hive.OpenTelemetry/Constants.EnvironmentVariables.cs` - Environment variable constants
- `hive.microservices/demo/Hive.MicroServices.Demo.Api/Program.cs` - Demo integration

## Dependency Graph

```
Hive.Abstractions (foundation)
    ├── Hive.Testing
    ├── Hive.Logging
    ├── Hive.OpenTelemetry
    └── Hive.MicroServices
            ├── Hive.MicroServices.Api
            ├── Hive.MicroServices.GraphQL
            ├── Hive.MicroServices.Grpc
            └── Hive.MicroServices.Job
```

All projects reference `Hive.Abstractions`. There are no circular dependencies.
