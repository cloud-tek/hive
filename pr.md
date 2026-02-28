## Summary

Replace `Hive.Testing` with the upstream `CloudTek.Testing` NuGet package (v10.0.1), eliminating 883 lines of duplicated test utility code.

### What changed

- **Added** `CloudTek.Testing` v10.0.1 as a centrally-managed NuGet dependency
- **Deleted 18 source files** from `Hive.Testing` — test attributes, discoverers, `SmartFact`/`SmartTheory`, `TestPortProvider`, `EnvironmentVariableScope`, `TestExecutionResolver`, enums, and internal constants (all now provided by `CloudTek.Testing`)
- **Deleted `Hive.Testing.Tests`** project — tests for trait attributes and `SmartFact` are now owned by the upstream package
- **Slimmed `Hive.Testing`** to a non-packable project containing only two Hive-specific extension classes:
  - `ConfigurationExtensions` — `UseEmbeddedConfiguration()`, `UseDefaultLoggingConfiguration()`
  - `MicroServiceTestExtensions` — `ShouldStart()`, `ShouldFailToStart()`
- **Removed `UseTestLogzIoConfiguration()`** — LogzIo has been dropped
- **Updated `using` statements** across 50 test files (`using Hive.Testing;` → `using CloudTek.Testing;`)

### Why

`Hive.Testing` duplicated all shared test utilities already maintained in `CloudTek.Testing`. This consolidation:
- Eliminates code duplication across repositories
- Reduces maintenance burden (attribute/discoverer changes only need to happen in one place)
- Gains the bonus `FeatureAttribute` for requirements traceability

### Impact

| Metric | Value |
|--------|-------|
| Files deleted | 22 (18 source + 4 test project) |
| Files modified | 52 (50 test files + csproj + Directory.Packages.props) |
| Net lines | +55 / -883 |
| `Hive.Testing` packable | No (was `true`, now `false`) |

## Test plan

- [x] `dotnet build Hive.sln` — 0 errors, 0 warnings
- [x] `dotnet test Hive.sln` — 395 tests pass, 0 failures
- [x] All `[UnitTest]`, `[IntegrationTest]`, `[SmartFact]`, `[SmartTheory]` attributes resolve from `CloudTek.Testing`
- [x] `TestPortProvider` and `EnvironmentVariableScope` resolve from `CloudTek.Testing`
- [x] `UseDefaultLoggingConfiguration()` and `ShouldStart()` still resolve from `Hive.Testing`