# Hive.HealthChecks — Code Optimization Analysis

## Overview

Analysis of code duplication and optimization opportunities in the `Hive.HealthChecks` module. Total duplicated code: **~96 lines** across source and test projects.

---

## Finding 1: Duplicated `ResolveOptions` + `ApplyConfigurationOverrides` (Critical)

**Files:**
- `hive.extensions/src/Hive.HealthChecks/HealthCheckStartupService.cs` (lines 94–130)
- `hive.extensions/src/Hive.HealthChecks/HealthCheckBackgroundService.cs` (lines 104–139)

**Description:** Both services contain identical private methods for resolving per-check options with the three-tier priority chain (explicit registration > IConfiguration > ConfigureDefaults > global defaults) and applying configuration overrides from `IConfiguration`.

**Duplicated lines:** 35 lines x 2 = **70 lines total**

**`ResolveOptions` (identical in both):**
```csharp
private HiveHealthCheckOptions ResolveOptions(Type checkType)
{
  if (_config.ExplicitRegistrations.TryGetValue(checkType, out var explicitOptions))
  {
    ApplyConfigurationOverrides(checkType, explicitOptions);
    return explicitOptions;
  }
  var options = new HiveHealthCheckOptions();
  ReflectionBridge.InvokeConfigureDefaults(checkType, options);
  ApplyConfigurationOverrides(checkType, options);
  return options;
}
```

**`ApplyConfigurationOverrides` (identical in both):**
```csharp
private void ApplyConfigurationOverrides(Type checkType, HiveHealthCheckOptions options)
{
  var checkName = ReflectionBridge.GetCheckName(checkType);
  var section = _config.Configuration.GetSection($"{HealthChecksOptions.SectionKey}:Checks:{checkName}");
  if (!section.Exists()) return;

  // 7 identical parsing blocks for: Interval, AffectsReadiness,
  // BlockReadinessProbeOnStartup, ReadinessThreshold,
  // FailureThreshold, SuccessThreshold, Timeout
}
```

### Recommended fix

Extract an internal `HealthCheckOptionsResolver` class injected via DI:

```csharp
internal sealed class HealthCheckOptionsResolver(HealthCheckConfiguration config)
{
  public HiveHealthCheckOptions ResolveOptions(Type checkType) { ... }
  private void ApplyConfigurationOverrides(Type checkType, HiveHealthCheckOptions options) { ... }
}
```

Both services replace their private methods with a call to the shared resolver.

---

## Finding 2: Duplicated Test `CreateConfig` Helper (Medium)

**Files:**
- `hive.extensions/tests/Hive.HealthChecks.Tests/HealthCheckStartupServiceTests.cs` (lines 13–25)
- `hive.extensions/tests/Hive.HealthChecks.Tests/HealthCheckBackgroundServiceTests.cs` (lines 13–25)

**Description:** Both test classes define an identical static `CreateConfig` factory method.

**Duplicated lines:** 13 lines x 2 = **26 lines total**

```csharp
private static HealthCheckConfiguration CreateConfig(
  IReadOnlyDictionary<Type, HiveHealthCheckOptions>? registrations = null,
  Dictionary<string, string?>? configValues = null)
{
  var config = new ConfigurationBuilder()
    .AddInMemoryCollection(configValues ?? [])
    .Build();

  return new HealthCheckConfiguration(
    new HealthChecksOptions(),
    registrations ?? new Dictionary<Type, HiveHealthCheckOptions>(),
    config);
}
```

### Recommended fix

Extract to a shared `TestHelpers.cs` in the test project:

```csharp
internal static class TestHelpers
{
  public static HealthCheckConfiguration CreateConfig(
    IReadOnlyDictionary<Type, HiveHealthCheckOptions>? registrations = null,
    Dictionary<string, string?>? configValues = null) { ... }
}
```

---

## Finding 3: Repetitive Configuration Parsing Pattern (Low)

**File:** Both `ApplyConfigurationOverrides` methods (see Finding 1)

**Description:** The same `section[key] is { } str && TryParse(str, out var v)` pattern repeats 7 times per method (14 times total). Each block follows an identical structure:

```csharp
if (section[nameof(HiveHealthCheckOptions.PropertyName)] is { } str
    && TypeParser.TryParse(str, out var value))
  options.PropertyName = value;
```

### Assessment

Not worth abstracting further. The repetition is mechanical but each line is self-explanatory. A data-driven approach (descriptor map + reflection) would trade clarity for brevity — not a net improvement for 7 properties.

---

## Clean Areas (No Action Needed)

| Component | Reason |
|---|---|
| `HealthCheckRegistry` | Unique state machine logic, no duplication |
| `ReflectionBridge` | Unique static abstract invocation bridge |
| `HealthCheckStartupGate` | Minimal, focused synchronization primitive |
| `HealthChecksBuilder` | Unique fluent builder API |
| `HealthChecksExtension` | Unique DI wiring and configuration |
| Test fakes | Appropriately distinct implementations |
| `EvaluateCheck` vs `OnStartAsync` | Different evaluation strategies, not duplicates |

---

## Summary

| # | Finding | Lines | Priority | Fix |
|---|---|---|---|---|
| 1 | Duplicated `ResolveOptions` + `ApplyConfigurationOverrides` | 70 | Critical | Extract `HealthCheckOptionsResolver` |
| 2 | Duplicated test `CreateConfig` helper | 26 | Medium | Extract shared `TestHelpers` class |
| 3 | Repetitive config parsing pattern | — | Low | Leave as-is (clarity > brevity) |
