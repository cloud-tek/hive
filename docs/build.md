# Build & CI Reference

All build targets run via `CloudTek.Build.Tool` (`cloudtek-build`), whose version is pinned in `.config/dotnet-tools.json` and restored by `dotnet tool restore`.

---

## Restore the build tool

```bash
dotnet tool restore
```

---

## CloudTek.Build.Tool targets

| Command | What it does |
|---|---|
| Target | What it does |
|---|---|
| `Restore` | Restore packages (depends on `Clean`) |
| `Clean` | Clean build outputs and artifacts |
| `Compile` | Build the solution |
| `UnitTests` | Run unit tests |
| `IntegrationTests` | Run integration tests |
| `Test` | Run unit and integration tests, plus `RunChecks` |
| `Pack` | Pack packable projects into NuGet packages in `artifacts/packages` |
| `Format` / `FormatCheck` | Apply / verify `dotnet format` |
| `OutdatedCheck` | Fail when packages are outdated |
| `BetaCheck` | Fail when pre-release packages are referenced |
| `VulnerabilityCheck` | Report vulnerable packages |
| `RunChecks` | Run all checks (commit lint, format, beta, outdated) |
| `All` (default) | Compile, test, pack and publish |

Run `dotnet cloudtek-build --help` for the full target list with dependencies.

```bash
# Full build
dotnet tool run cloudtek-build --target All

# Only unit tests
dotnet tool run cloudtek-build --target UnitTests

# The CI-style gate
dotnet tool run cloudtek-build --target RunChecks Compile UnitTests

# Compile and test only — skip the RunChecks phase (faster for local iteration)
dotnet tool run cloudtek-build --target All --Skip RunChecks
```

`--target` and `--Skip` accept one or more target names separated by spaces (NUKE convention).

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

The attributes are provided by the `CloudTek.Testing` NuGet package. Add `[Feature("ARTIFACT/REQ")]` to trace a test to a requirement.

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