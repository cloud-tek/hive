# Release Notes - Hive 10.0.0

## Overview

Hive 10.0.0 is a major release that migrates the framework from .NET 8 to .NET 10, replaces Serilog with OpenTelemetry, introduces new extension modules, and adds Azure Functions support. This release contains breaking changes.

## What's New

### .NET 10 and C# 14
- All projects target `net10.0`
- C# 14 language features available (extension members, `field` keyword, implicit span conversions)

### New Modules

- **Hive.OpenTelemetry** - Unified logging, tracing, and metrics via OTLP. Replaces the Serilog-based logging stack.
- **Hive.Functions** - Azure Functions Worker integration via `IFunctionHost`, sharing the same extension pattern as `IMicroService`.
- **Hive.Messaging** + **Hive.Messaging.RabbitMq** - Messaging abstractions built on Wolverine with RabbitMQ transport, readiness middleware, and telemetry.
- **Hive.HealthChecks** - Threshold-based readiness gating with background health monitoring and startup gate.
- **Hive.HTTP** + **Hive.HTTP.Testing** - Typed HTTP clients with Refit, resilience policies, authentication, and telemetry.
- **Hive.MicroServices.Testing** - `WebApplicationFactory`-based integration testing utilities.

### Architecture Improvements

- **`IMicroServiceCore`** - New hosting-model-agnostic base interface. Extensions now depend on `IMicroServiceCore` instead of `IMicroService`, enabling reuse across ASP.NET and Azure Functions hosts.
- **`IActivitySourceProvider`** - Cross-extension tracing discovery. Extensions implementing this interface have their activity sources automatically subscribed by `Hive.OpenTelemetry`.
- **`ConfigurationBuilderFactory`** - Shared configuration loading standardized across `MicroService` and `FunctionHost`.
- **Compile-time extension safety** - `MicroServiceExtension<TExtension>` uses the Curiously Recurring Template Pattern with `static abstract` factory methods for type-safe `RegisterExtension<T>()`.

### Build System

- **CloudTek.Build.Tool** replaces NUKE for builds (`dotnet tool run cloudtek-build --target All`)
- **Version.targets** replaces GitVersion for version management
- Node.js dependencies (commitlint, package.json) removed

### Aspire Integration

- Demo AppHost orchestrates all demo services with OTel Collector fan-out to Aspire Dashboard and VictoriaMetrics.

## Breaking Changes

### Target Framework
- All packages now target `net10.0`. Consumers must upgrade to the .NET 10 SDK.

### Serilog Removed
- `Hive.Logging`, `Hive.Logging.AppInsights`, `Hive.Logging.LogzIo`, and `Hive.Logging.Xunit` packages are deleted.
- Replace `.WithSerilog()` / `.WithLogging()` with `.WithOpenTelemetry()`.

### Interface Changes
- `IMicroServiceCore` is the new base interface; `IMicroService` extends it.
- Extension constructors now take `IMicroServiceCore` instead of `IMicroService`.
- `MicroServiceExtension` constructor: `IMicroServiceCore service` (was `IMicroService`).
- `ConfigureServices` signature: `IServiceCollection ConfigureServices(IServiceCollection, IMicroServiceCore)` (was `void ConfigureServices(IServiceCollection, IConfiguration)`).
- `Configure` signature: `IApplicationBuilder Configure(IApplicationBuilder, IMicroServiceCore)` (was `void Configure(IApplicationBuilder)`).

### Hive.Testing
- No longer published as a NuGet package (`IsPackable=false`).
- Custom xUnit test attributes (`[UnitTest]`, `[IntegrationTest]`, etc.) moved to the `CloudTek.Testing` NuGet package.
- Replace `using Hive.Testing;` with `using CloudTek.Testing;` and add a package reference to `CloudTek.Testing`.

### Package Upgrades
- FluentAssertions: 6.x to 7.x (API changes)
- FluentValidation: 11.x to 12.x
- HotChocolate: 12.x to 15.x
- xunit: 2.4.x to 2.9.x
- Scrutor: 4.x to 7.x

### Build System
- NUKE build scripts removed. Use `dotnet tool run cloudtek-build --target All` or plain `dotnet build` / `dotnet test`.
- GitVersion removed. Version is managed via `Version.targets`.

## Migration Guide

### 1. Update SDK and Target Framework
```xml
<!-- global.json -->
{ "sdk": { "version": "10.0.100" } }

<!-- Directory.Build.props or .csproj -->
<TargetFramework>net10.0</TargetFramework>
```

### 2. Replace Serilog with OpenTelemetry
```csharp
// Before (8.x)
var service = new MicroService("my-service")
    .WithLogging(builder => { })
    .ConfigureApiPipeline(app => { });

// After (10.0.0)
var service = new MicroService("my-service")
    .WithOpenTelemetry()
    .ConfigureApiPipeline(app => { });
```

### 3. Update Custom Extensions
```csharp
// Before (8.x)
public class MyExtension : MicroServiceExtension
{
    public MyExtension(IMicroService service) : base(service) { }

    public override void ConfigureServices(IServiceCollection services, IConfiguration config) { }
    public override void Configure(IApplicationBuilder app) { }
}

// After (10.0.0)
public class MyExtension : MicroServiceExtension<MyExtension>
{
    public MyExtension(IMicroServiceCore service) : base(service) { }

    public override IServiceCollection ConfigureServices(
        IServiceCollection services, IMicroServiceCore microservice)
    {
        return services;
    }

    public override IApplicationBuilder Configure(
        IApplicationBuilder app, IMicroServiceCore microservice)
    {
        return app;
    }
}
```

### 4. Update Test Attributes
```xml
<!-- Replace Hive.Testing package reference -->
<PackageReference Include="CloudTek.Testing" />
```
```csharp
// Replace using
using CloudTek.Testing; // was: using Hive.Testing;
```

### 5. Update Package Versions
Review `Directory.Packages.props` for updated versions of FluentAssertions, FluentValidation, HotChocolate, and other dependencies. See the full list in the Breaking Changes section above.