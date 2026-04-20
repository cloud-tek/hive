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

## File References

When working with code, reference files using this format:

- Files: `Hive.Abstractions/IMicroService.cs`
- Specific lines: `Hive.MicroServices/MicroService.cs:42`
- Ranges: `Hive.MicroServices.Api/IMicroServiceExtensions.cs:10-25`

## Important Paths

- Core abstractions: `hive.core/src/Hive.Abstractions/`
- Main framework: `hive.microservices/src/Hive.MicroServices/`
- OpenTelemetry extension: `hive.opentelemetry/src/Hive.OpenTelemetry/`
- Messaging extension: `hive.extensions/src/Hive.Messaging/`
- Health checks extension: `hive.extensions/src/Hive.HealthChecks/`
- Demo applications: `hive.microservices/demo/`
- Test projects: `*/tests/`
