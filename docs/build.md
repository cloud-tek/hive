# Build & CI Reference

All build targets run via `CloudTek.Build.Tool` (`cloudtek-build`), which is declared in `.config/dotnet-tools.json` at version `10.0.0`.

---

## Restore the build tool

```bash
dotnet tool restore
```

---

## CloudTek.Build.Tool targets

| Command | What it does |
|---|---|
| `dotnet tool run cloudtek-build --target All` | Full build: compile, test, and all checks (outdated packages, formatting, etc.) |
| `dotnet tool run cloudtek-build --target All --Skip RunChecks` | Compile and test only — skips the `RunChecks` phase (faster for local iteration) |

The `--Skip` flag accepts a comma-separated list of target names to exclude.

---

## dotnet CLI equivalents

Use these when the build tool is not available or for targeted operations.

### Build

```bash
# Entire solution
dotnet build Hive.sln

# Release configuration
dotnet build Hive.sln -c Release

# Single project
dotnet build hive.microservices/src/Hive.MicroServices/Hive.MicroServices.csproj
```

### Test

```bash
# All tests
dotnet test Hive.sln

# By category (xUnit trait filter)
dotnet test Hive.sln --filter Category=UnitTests
dotnet test Hive.sln --filter Category=IntegrationTests
dotnet test Hive.sln --filter Category=ModuleTests
dotnet test Hive.sln --filter Category=SmokeTests
dotnet test Hive.sln --filter Category=SystemTests

# Single test by fully-qualified name
dotnet test Hive.sln --filter FullyQualifiedName~MyTestClass.MyTestMethod
```

### Test category attributes

Hive defines custom xUnit attributes in `CloudTek.Testing` (exposed via `Hive.Testing`):

| Attribute | `Category` value | Typical use |
|---|---|---|
| `[UnitTest]` | `UnitTests` | Fast, in-process, no I/O |
| `[IntegrationTest]` | `IntegrationTests` | Real dependencies or TestServer |
| `[ModuleTest]` | `ModuleTests` | Module-scope cross-cutting tests |
| `[SmokeTest]` | `SmokeTests` | Quick sanity checks |
| `[SystemTest]` | `SystemTests` | End-to-end against a running service |

---

## Running demo applications

```bash
# REST API demo
dotnet run --project hive.microservices/demo/Hive.MicroServices.Demo.Api

# All demos via Aspire orchestration
dotnet run --project hive.microservices/demo/Hive.MicroServices.Demo.Aspire
```

---

## Centralized package management

All NuGet package versions are declared in `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`). Project files reference packages without a `Version` attribute. To add a new package:

1. Add a `<PackageVersion Include="..." Version="..." />` entry to `Directory.Packages.props`.
2. Add `<PackageReference Include="..." />` (no version) to the target `.csproj`.

Version source of truth for Hive's own packages: [`Version.targets`](../Version.targets).

---

## CI notes

- The Dagger-based CI `validate` workflow has a 128 MB patch limit when `--auto-apply` is used. See [dagger.md](dagger.md) for the workaround.
- Outdated-package checks run as part of `RunChecks`; skip with `--Skip RunChecks` to avoid failures caused by upstream releases during development.