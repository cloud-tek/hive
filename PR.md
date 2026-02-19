# Add Azure Functions Support with Major Architectural Improvements

## Summary

This PR introduces comprehensive Azure Functions integration to the Hive framework through a new `hive.functions` module, alongside significant architectural improvements, OpenTelemetry migration, and infrastructure enhancements.

**Branch:** `feature/functions` → `release/10.0.0`
**Changes:** 194 files changed, 20,808 insertions(+), 6,647 deletions(-)

## Key Features

### 🚀 Azure Functions Integration

- **New Module:** `hive.functions` with complete Azure Functions support
- **IFunctionHost Interface:** Framework-agnostic function host abstraction
- **FunctionHost Implementation:** Production-ready Azure Functions host with full extension system support
- **Demo Application:** Working Azure Functions demo with weather service and OpenTelemetry integration
- **Extension Support:** All Hive extensions (OpenTelemetry, CORS, etc.) work seamlessly with Azure Functions

**New Projects:**
- `hive.functions/src/Hive.Functions` - Core Functions integration library
- `hive.functions/demo/Hive.Functions.Demo` - Demonstration Azure Functions app
- `hive.functions/tests/Hive.Functions.Tests` - Unit tests

### 🏗️ Major Architectural Refactoring

**IMicroServiceCore Interface**
- Extracted framework-agnostic base interface from `IMicroService`
- Supports multiple hosting models (ASP.NET, Azure Functions, future hosts)
- Core properties: `Name`, `Id`, `Environment`, `ConfigurationRoot`, `EnvironmentVariables`, `Args`
- Extension system: `Extensions` list, `RegisterExtension<T>()`
- Lifecycle management: `InitializeAsync()`, `StartAsync()`, `StopAsync()`

**Interface Hierarchy:**
```
IMicroServiceCore (framework-agnostic base)
  ├── IMicroService (ASP.NET microservices)
  └── IFunctionHost (Azure Functions)
```

**Compile-Time Safety for Extensions**
- Enforced CRTP (Curiously Recurring Template Pattern) for all extensions
- Extensions must inherit from `MicroServiceExtension<TExtension>` where `TExtension` is the extension type itself
- `RegisterExtension<T>()` provides compile-time validation
- Prevents runtime errors from improperly implemented extensions

### 📊 OpenTelemetry Migration

**Complete Serilog Replacement**
- Removed entire `hive.logging` module (Serilog, AppInsights, LogzIo integrations)
- New `hive.opentelemetry` module with unified observability
- OTLP protocol support for logs, traces, and metrics
- Environment-based configuration via `OTEL_EXPORTER_OTLP_ENDPOINT`

**Features:**
- FluentValidation-based configuration validation
- Comprehensive E2E tests using InMemoryExporter
- Resource attribute correlation (`service.name`, `service.instance.id`)
- ASP.NET Core, HTTP client, and runtime instrumentation
- Console export fallback when OTLP endpoint not configured

**New Projects:**
- `hive.opentelemetry/src/Hive.OpenTelemetry` - OpenTelemetry extension
- `hive.opentelemetry/tests/Hive.OpenTelemetry.Tests` - Comprehensive test suite

### 🔒 CORS Improvements

- **Refactored to Hive.Abstractions:** CORS configuration now available framework-wide
- **Critical Bug Fixes:** Resolved validation issues in CORS middleware
- **Enhanced Testing:** Comprehensive integration tests for all CORS scenarios
- **Improved Documentation:** Detailed CORS configuration guide with examples

**Test Scenarios:**
- Restrictive policies with specific origins
- Wildcard policies with `AllowAny`
- Multiple origins with credentials
- Pre-flight request handling

### 🧪 Testing Infrastructure

**New Hive.MicroServices.Testing Library**
- Custom xUnit trait attributes: `[UnitTest]`, `[IntegrationTest]`, `[ModuleTest]`, `[SmokeTest]`, `[SystemTest]`
- WebApplicationFactory integration for ASP.NET testing
- Testing utilities: `TestPortProvider`, `EnvironmentVariableScope`
- Extension methods: `.InTestClass<T>()`, `.ShouldStart()`, `.ShouldFailToStart()`

**Enhanced Test Coverage:**
- OpenTelemetry: 600+ test cases across configuration, E2E scenarios, and resource correlation
- CORS: Integration tests for all policy configurations
- Azure Functions: Host initialization and lifecycle tests

### 🔧 Repository Infrastructure

**Centralized Version Management**
- New `Version.targets` file as single source of truth for package versions
- All packages reference centralized version via MSBuild imports
- Consistent versioning across all artifacts

**Build System Migration**
- Migrated from NUKE to CloudTek.Build.Tool
- Removed GitVersion and npm dependencies
- Simplified build process: `dotnet tool run cloudtek-build --target All`

**Repository Policies**
- Comprehensive module structure rules in `.claude/rules/repository-policies.md`
- Enforced project location patterns: `{module}/{subfolder}/{ProjectName}/{ProjectName}.csproj`
- Module naming conventions (lowercase with dots: `hive.core`, `hive.microservices`)

**Updated Dependencies**
- .NET 10 SDK
- CloudTek.Sdk 10.0.0-beta.5
- Latest NuGet packages across all projects

### 🤖 CI/CD Enhancements

**Updated Workflows**
- `dotnet-build.yml` - Build and test automation
- `dotnet-validate-pr.yml` - PR validation
- `claude-code-review.yml` - AI-powered code review
- `release-notes.yml` - Automated release notes generation

**Removed Legacy**
- Removed NUKE workflows (`nuke.yml`, `nuke.old.yml`)
- Removed old CI workflow (`ci.yml`)

### 📚 Documentation

**New Documentation Files**
- `README.md` - Repository overview and getting started
- `CLAUDE.md` - Comprehensive guide for Claude Code integration
- `HIVE_FUNCTIONS_DESIGN.md` - Azure Functions architecture and design decisions
- `hive.microservices/README.md` - MicroServices module documentation
- `hive.microservices/src/Hive.MicroServices/CORS/README.md` - CORS configuration guide
- `hive.opentelemetry/README.md` - OpenTelemetry integration guide
- `hive.opentelemetry/CONFIGURATION_STRATEGY.md` - Configuration patterns
- `hive.microservices/src/Hive.MicroServices.Testing/README.md` - Testing guide

**Enhanced Module Documentation**
- Architecture overviews for all major modules
- Testing strategies and examples
- Configuration patterns and best practices
- API reference and usage examples

## Breaking Changes

### ⚠️ Removed Modules

**hive.logging** - Complete removal
- All Serilog-based logging removed
- AppInsights integration removed (use OpenTelemetry)
- LogzIo integration removed (use OpenTelemetry)

**Migration Path:**
```csharp
// Old (Serilog)
new MicroService("my-service")
    .WithLogging(config => config.WithAppInsights(...))

// New (OpenTelemetry)
new MicroService("my-service")
    .WithOpenTelemetry(
        logging: builder => { },
        tracing: builder => { },
        metrics: builder => { }
    )
```

### ⚠️ Interface Changes

**IMicroService**
- Now inherits from `IMicroServiceCore`
- ASP.NET-specific members remain in `IMicroService`
- Extensions that work across hosting models should accept `IMicroServiceCore`

**MicroServiceExtension**
- Now requires generic type parameter: `MicroServiceExtension<TExtension>`
- Extensions must use CRTP pattern for compile-time safety
- Factory method `Create` inherited from base class

**Migration Path:**
```csharp
// Old
public class MyExtension : MicroServiceExtension
{
    public MyExtension(IMicroService service) : base(service) { }
}

// New
public class MyExtension : MicroServiceExtension<MyExtension>
{
    public MyExtension(IMicroServiceCore service) : base(service) { }
}
```

### ⚠️ Build System

**NUKE Build Removed**
- All NUKE build files removed (`build/`, `build.ps1`, `build.sh`, `build.cmd`)
- GitVersion removed
- npm dependencies removed (`package.json`, `package-lock.json`)

**Migration Path:**
```bash
# Old
./build.sh

# New
dotnet tool run cloudtek-build --target All
```

### ⚠️ CORS Configuration

**Moved to Hive.Abstractions**
- CORS types moved from `Hive.MicroServices.CORS` to `Hive.Abstractions.Configuration.CORS`
- Namespace changes may require using directive updates

## Testing

### Test Coverage

**OpenTelemetry (600+ tests)**
- Configuration validation tests
- E2E log emission tests
- E2E trace emission tests
- E2E metrics emission tests
- Resource correlation tests
- Context propagation tests

**CORS Integration Tests**
- Restrictive policy enforcement
- Wildcard policy behavior
- Multiple origins handling
- Pre-flight request validation

**Azure Functions Tests**
- Host initialization
- Extension registration
- Service lifecycle

### Running Tests

```bash
# Run all tests
dotnet test Hive.sln

# Run specific test categories
dotnet test --filter Category=UnitTests
dotnet test --filter Category=IntegrationTests

# Run OpenTelemetry tests
dotnet test hive.opentelemetry/tests/Hive.OpenTelemetry.Tests

# Run Functions tests
dotnet test hive.functions/tests/Hive.Functions.Tests
```

### Build Verification

```bash
# Full build with all checks
dotnet tool run cloudtek-build --target All

# Quick build (skip checks)
dotnet tool run cloudtek-build --target All --skip RunChecks
```

## Demo Applications

### Azure Functions Demo

```bash
cd hive.functions/demo/Hive.Functions.Demo
dotnet run
```

Features:
- Weather forecast HTTP trigger function
- OpenTelemetry integration (logs, traces, metrics)
- Dependency injection
- Configuration management

### ASP.NET Microservices Demo

```bash
# Run individual demos
dotnet run --project hive.microservices/demo/Hive.MicroServices.Demo.Api
dotnet run --project hive.microservices/demo/Hive.MicroServices.Demo.Job

# Run Aspire orchestration (all demos)
dotnet run --project hive.microservices/demo/Hive.MicroServices.Demo.Aspire
```

## Migration Guide

### For Existing Projects Using Hive.Logging

1. Remove Serilog dependencies:
```xml
<!-- Remove these -->
<PackageReference Include="Hive.Logging" />
<PackageReference Include="Hive.Logging.AppInsights" />
```

2. Add OpenTelemetry:
```xml
<PackageReference Include="Hive.OpenTelemetry" />
```

3. Update service configuration:
```csharp
// Replace WithLogging with WithOpenTelemetry
new MicroService("my-service")
    .WithOpenTelemetry(
        logging: builder =>
        {
            // Customize logging if needed
        },
        tracing: builder =>
        {
            // Add custom trace sources
            builder.AddSource("MyApp.*");
        }
    )
```

4. Set OTLP endpoint (optional):
```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

### For Extension Authors

1. Update extension base class:
```csharp
public class MyExtension : MicroServiceExtension<MyExtension>
{
    public MyExtension(IMicroServiceCore service) : base(service) { }

    // Implementation...
}
```

2. Update to use `IMicroServiceCore` if supporting multiple hosting models
3. Add factory method tests to verify compile-time safety

## Checklist

- [x] All tests pass (`dotnet test Hive.sln`)
- [x] Build succeeds (`dotnet tool run cloudtek-build --target All`)
- [x] Breaking changes documented
- [x] Migration guide provided
- [x] Demo applications updated
- [x] Comprehensive documentation added
- [x] Azure Functions integration tested
- [x] OpenTelemetry migration complete
- [x] CORS improvements validated
- [x] Repository policies enforced
- [x] CI/CD workflows updated

## Additional Notes

### Design Documents

- **HIVE_FUNCTIONS_DESIGN.md** - Detailed Azure Functions architecture decisions, interface hierarchy rationale, and extension system design
- **.github/claude.proposed-strategy.md** - Claude Code integration strategy
- **.github/git-hooks-analysis.md** - Git hooks analysis and recommendations

### Future Enhancements

- Additional Azure Functions triggers (Queue, Timer, Event Grid)
- Durable Functions support
- Additional hosting models (AWS Lambda, Google Cloud Functions)
- Enhanced observability dashboards
- Performance benchmarking suite

## Review Focus Areas

1. **Interface Design** - Review `IMicroServiceCore` abstraction and its impact on existing code
2. **Breaking Changes** - Ensure migration path is clear for Serilog → OpenTelemetry
3. **Azure Functions Integration** - Validate FunctionHost implementation and extension compatibility
4. **Test Coverage** - Review 600+ new tests for OpenTelemetry and ensure adequate coverage
5. **Documentation** - Verify all new features are adequately documented

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
