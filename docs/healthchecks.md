# Hive.HealthChecks Design Document

## Status: ALL DESIGN QUESTIONS RESOLVED — Ready for Implementation

## 1. Problem Statement

Hive microservices need a structured, strongly-typed health check system that:

- Evaluates dependency health **in the background** so probe traffic doesn't cascade into downstream calls
- Exposes the **last known state** of each health check via the readiness probe (`/status/readiness`)
- Drives `IMicroService.IsReady` based on aggregated health check results
- Provides reusable abstractions for building specialized health checks (e.g., RabbitMQ, database, HTTP)
- Integrates seamlessly with the existing Hive extension system

## 2. Current State Analysis

### What exists today

| Aspect | Current State |
|--------|--------------|
| `IsReady` | Simple boolean, set to `true` once at startup, never toggled back |
| Readiness probe | Returns `{ "ready": true/false }` — no granularity |
| `MicroServiceExtension.ConfigureHealthChecks()` | **Dead code** — defined on base class but never invoked by the pipeline |
| Messaging health checks | `RabbitMqTransportProvider.ConfigureHealthChecks()` registers ASP.NET Core `IHealthCheck` instances, but they are never called |
| Background evaluation | None — health checks would execute synchronously on probe requests if the wiring existed |

### Key insight

The existing `ConfigureHealthChecks(IHealthChecksBuilder)` virtual method on `MicroServiceExtension` and its override in `MessagingExtensionBase` form a **dead code path**. `MicroService.ConfigureExtensions()` calls `ConfigureServices` and `Configure` on each extension, but never calls `ConfigureHealthChecks`. This design introduces a **replacement** for that mechanism.

## 3. Proposed Architecture

### 3.1 Project Layout

```
hive.core/
  src/
    Hive.Abstractions/
      HealthChecks/                         # Read-only contracts + options POCO
        HealthCheckStatus.cs                # Enum: Healthy, Degraded, Unhealthy, Unknown
        HealthCheckStateSnapshot.cs         # Read-only record for probe response
        HiveHealthCheckOptions.cs           # Per-check configuration (POCO — used by IHiveHealthCheck.ConfigureDefaults)
        IHealthCheckStateProvider.cs        # Interface resolved by ReadinessMiddleware
        IHiveHealthCheck.cs                 # Interface with static abstract ConfigureDefaults
        ReadinessThreshold.cs               # Enum: Degraded, Healthy

hive.extensions/
  src/
    Hive.HealthChecks/                      # Full runtime
      Hive.HealthChecks.csproj
      Startup.cs                            # WithHealthChecks() IMicroService extension method
      HealthChecksBuilder.cs                # Callback builder: exposes WithHealthCheck<T>() inside WithHealthChecks()
      HealthChecksExtension.cs              # MicroServiceExtension<HealthChecksExtension>
      HealthCheckStartupService.cs          # IHostedStartupService — blocking startup evaluation
      HealthCheckBackgroundService.cs       # BackgroundService — independent timer loops
      HealthCheckRegistry.cs                # Thread-safe registry (lock-based), implements IHealthCheckStateProvider
      HealthCheckState.cs                   # Mutable per-check state (internal)
      HiveHealthCheck.cs                    # Abstract base class for extension authors
      HiveHealthCheck{TOptions}.cs          # Options-aware variant
      HealthChecksOptions.cs                # Global configuration
```

### 3.2 Core Abstractions

#### `HealthCheckStatus` enum

```csharp
public enum HealthCheckStatus
{
  Unknown,    // Not yet evaluated
  Healthy,
  Degraded,   // Operational but suboptimal
  Unhealthy   // Dependency down
}
```

#### `HiveHealthCheck` — abstract base class (in `Hive.HealthChecks`)

```csharp
public abstract class HiveHealthCheck : IHiveHealthCheck
{
  /// <summary>
  /// Instance-level name. Defaults to delegating to the static CheckName
  /// via a reflection bridge (see GetCheckName helper). Concrete subclasses
  /// only need to implement the static CheckName property.
  /// </summary>
  public virtual string Name => ReflectionBridge.GetCheckName(GetType());

  /// <summary>
  /// Evaluate the health of the dependency. Dependencies are
  /// constructor-injected (health checks are DI singletons).
  /// For scoped services (e.g., DbContext), inject IServiceScopeFactory.
  /// </summary>
  public abstract Task<HealthCheckStatus> EvaluateAsync(CancellationToken ct);

  // CheckName and ConfigureDefaults are static abstracts on IHiveHealthCheck — see Q5.
  // Concrete subclasses MUST implement both. No instance needed — called as
  // T.CheckName / T.ConfigureDefaults() during ConfigureServices.
}
```

#### `HiveHealthCheck<TOptions>` — options-aware variant (in `Hive.HealthChecks`)

```csharp
public abstract class HiveHealthCheck<TOptions> : HiveHealthCheck
  where TOptions : class, new()
{
  /// <summary>
  /// Check-specific options, bound from IConfiguration by convention:
  /// Hive:HealthChecks:Checks:{CheckName}:Options
  /// where {CheckName} is the static abstract property on IHiveHealthCheck.
  /// </summary>
  public TOptions Options { get; internal set; } = new();
}
```

**Convention-based binding:** During `ConfigureServices`, the framework detects that a check inherits from `HiveHealthCheck<TOptions>` and binds `TOptions` from the IConfiguration section `Hive:HealthChecks:Checks:{CheckName}:Options`. No explicit section key needed — the check's static `CheckName` property determines the config path (available without an instance).

Example:
```csharp
public sealed class RabbitMqHealthCheck : HiveHealthCheck<RabbitMqHealthCheckOptions>
{
  public static string CheckName => "RabbitMq";
  // Name inherited from base class, delegates to CheckName → "RabbitMq"
  // Options.ManagementApiUri bound from Hive:HealthChecks:Checks:RabbitMq:Options:ManagementApiUri

  public static void ConfigureDefaults(HiveHealthCheckOptions options)
  {
    options.AffectsReadiness = true;
    options.BlockReadinessProbeOnStartup = true;
  }

  public override async Task<HealthCheckStatus> EvaluateAsync(CancellationToken ct)
  {
    // IConnection constructor-injected; Options.ManagementApiUri available
  }
}

public sealed class RabbitMqHealthCheckOptions
{
  public string? ManagementApiUri { get; set; }
}
```

```json
{
  "Hive": {
    "HealthChecks": {
      "Checks": {
        "RabbitMq": {
          "IntervalSeconds": 15,
          "AffectsReadiness": true,
          "Options": {
            "ManagementApiUri": "http://localhost:15672"
          }
        }
      }
    }
  }
}
```

### 3.3 Registration API

```csharp
// Fluent API — callback-based, matches WithMessaging() / WithOpenTelemetry() patterns
var service = new MicroService("my-service")
  .WithOpenTelemetry()
  .WithMessaging(msg => { ... })
  .WithHealthChecks(checks =>
  {
    checks.Interval = TimeSpan.FromSeconds(30);

    checks.WithHealthCheck<RabbitMqHealthCheck>(cfg =>
    {
      cfg.AffectsReadiness = true;
      cfg.BlockReadinessProbeOnStartup = true;  // default
    });

    checks.WithHealthCheck<DatabaseHealthCheck>(cfg =>
    {
      cfg.Interval = TimeSpan.FromMinutes(1); // override global interval
      cfg.AffectsReadiness = false;           // informational only
      cfg.BlockReadinessProbeOnStartup = false;
    });
  })
  .ConfigureApiPipeline(app => { });
```

#### Ordering enforcement via callback scope

`WithHealthCheck<T>()` is a method on the `HealthChecksBuilder` received inside the `WithHealthChecks()` callback. It is **not accessible** outside this callback — ordering is enforced by scope, not by return types:

```csharp
// In Startup.cs:
public static IMicroService WithHealthChecks(
  this IMicroService service, Action<HealthChecksBuilder>? configure = null);

// HealthChecksBuilder — received inside the callback
public sealed class HealthChecksBuilder
{
  public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
  public bool DisableAutoDiscovery { get; set; }

  public HealthChecksBuilder WithHealthCheck<T>(
    Action<HiveHealthCheckOptions>? configure = null)
    where T : class, IHiveHealthCheck;
}
```

This means:
- `WithHealthChecks()` returns `IMicroService` — standard Hive pattern, no wrapper types
- `WithHealthCheck<T>()` is only accessible inside the callback — ordering naturally enforced
- Consistent with existing patterns: `WithMessaging(msg => { ... })`, `WithOpenTelemetry(...)`
- No `IHealthCheckBuilder` interface or delegation boilerplate needed

**Additional rules:**
- `.WithHealthCheck<T>()` can be called multiple times for different check types
- Each check type can only be registered once (runtime guard against duplicates)

### 3.4 Configuration via `IConfiguration`

```json
{
  "Hive": {
    "HealthChecks": {
      "IntervalSeconds": 30,
      "Checks": {
        "RabbitMq": {
          "IntervalSeconds": 15,
          "AffectsReadiness": true,
          "BlockReadinessProbeOnStartup": true
        }
      }
    }
  }
}
```

Configuration merging priority: `IConfiguration` > fluent API overrides > `T.ConfigureDefaults()` (static abstract) > global defaults.

### 3.5 Health Check Evaluation (two-phase)

Evaluation happens in two distinct phases:

#### Phase 1: Startup (synchronous, blocking)

Runs as `IHostedStartupService.StartAsync()` — **before** `IsReady` is set.

```
StartupService.ExecuteHostedStartupServices()
    │
    ├── HealthCheckStartupService.StartAsync()
    │     │
    │     ├── for each check where BlockReadinessProbeOnStartup=true:
    │     │     1. Resolve check from DI
    │     │     2. Call EvaluateAsync()
    │     │     3. Write result to HealthCheckState immediately
    │     │     4. If Unhealthy → throw (startup fails)
    │     │
    │     └── Skip checks where BlockReadinessProbeOnStartup=false
    │
    ▼
IsStarted=true, IsReady=true, ServiceStarted signaled
```

#### Phase 2: Background (independent timers)

Runs as `BackgroundService.ExecuteAsync()` — after startup completes. One `PeriodicTimer` per check.

```
HealthCheckBackgroundService.ExecuteAsync()
    │
    ├── Task: RabbitMqHealthCheck (every 15s)
    │     └── tick → EvaluateAsync() → update state → recompute IsReady
    │
    ├── Task: DatabaseHealthCheck (every 60s)
    │     └── tick → EvaluateAsync() → update state → recompute IsReady
    │
    └── Task: MetricsCollectorHealthCheck (every 30s)
          └── tick → EvaluateAsync() → update state → recompute IsReady
```

Each check's timer is independent. `IsReady` is recomputed after **each individual check** completes — not batched across checks.

### 3.6 Readiness Probe Response (enriched)

The `ReadinessMiddleware` response is enriched to include health check detail:

```json
{
  "name": "my-service",
  "id": "abc-123",
  "hostingMode": "Kubernetes",
  "pipelineMode": "Api",
  "started": true,
  "ready": true,
  "checks": [
    {
      "name": "RabbitMq",
      "status": "Healthy",
      "lastCheckedAt": "2026-02-26T12:00:00Z",
      "durationMs": 42,
      "affectsReadiness": true,
      "readinessThreshold": "Degraded",
      "consecutiveFailures": 0,
      "consecutiveSuccesses": 12,
      "isPassingForReadiness": true
    },
    {
      "name": "Database",
      "status": "Degraded",
      "lastCheckedAt": "2026-02-26T11:59:30Z",
      "durationMs": 150,
      "affectsReadiness": false,
      "readinessThreshold": "Degraded",
      "consecutiveFailures": 0,
      "consecutiveSuccesses": 5,
      "isPassingForReadiness": true
    }
  ]
}
```

When no health checks are registered, the response remains unchanged (backward compatible).

### 3.7 Integration with Existing Extensions

Existing extensions like `Hive.Messaging.RabbitMq` ship their own `HiveHealthCheck` implementations. These are **auto-discovered by Scrutor** — no manual registration needed:

```csharp
// In Hive.Messaging.RabbitMq — auto-discovered, no wiring on the extension class
public sealed class RabbitMqHealthCheck : HiveHealthCheck<RabbitMqHealthCheckOptions>
{
  public static string CheckName => "RabbitMq";
  // Name inherited from base class → "RabbitMq"

  public static void ConfigureDefaults(HiveHealthCheckOptions options)
  {
    options.AffectsReadiness = true;
    options.BlockReadinessProbeOnStartup = true;
  }

  public override async Task<HealthCheckStatus> EvaluateAsync(
    CancellationToken ct)
  {
    // IConnection is constructor-injected (DI singleton)
    // Options.ManagementApiUri available via HiveHealthCheck<TOptions>.Options
  }
}
```

Users just enable health checks — RabbitMQ check is discovered automatically:
```csharp
.WithMessaging(msg => { ... })
.WithHealthChecks(checks => { })  // RabbitMqHealthCheck auto-discovered
.ConfigureApiPipeline(app => { });
```

To override auto-discovered defaults:
```csharp
.WithHealthChecks(checks =>
{
  checks.WithHealthCheck<RabbitMqHealthCheck>(cfg =>
  {
    cfg.FailureThreshold = 3; // override the auto-discovered defaults
  });
})
```

## 4. Resolved Design Questions

The following questions were resolved during the design phase. Each is numbered for reference.

---

### ~~Q1: Should `Degraded` status affect readiness?~~ RESOLVED

**Decision:** Configurable per health check via `ReadinessThreshold` enum, with a consecutive-failure threshold. Default: `Degraded` does **not** affect readiness (matches ASP.NET Core convention where `Degraded` maps to HTTP 200).

#### ReadinessThreshold enum

```csharp
/// <summary>
/// The minimum HealthCheckStatus that counts as "passing" for readiness.
/// </summary>
public enum ReadinessThreshold
{
  /// <summary>
  /// Both Healthy and Degraded are considered passing.
  /// Only Unhealthy removes readiness. This is the default.
  /// </summary>
  Degraded,

  /// <summary>
  /// Only Healthy is considered passing.
  /// Both Degraded and Unhealthy remove readiness.
  /// </summary>
  Healthy
}
```

The semantics: a check "passes" for readiness if its status is **at or above** the threshold. `ReadinessThreshold.Degraded` (default) means both `Healthy` and `Degraded` pass. `ReadinessThreshold.Healthy` means only `Healthy` passes.

#### Failure and recovery thresholds

A check does not immediately affect readiness when it drops below its `ReadinessThreshold`. Instead, it must fail **consecutively** a configurable number of times (mirroring Kubernetes' `failureThreshold` concept). Similarly, recovery from a failed state requires consecutive passing results:

```csharp
public sealed class HiveHealthCheckOptions
{
  // ... other properties ...

  /// <summary>
  /// Minimum status for this check to be considered passing for readiness.
  /// Default: ReadinessThreshold.Degraded (both Healthy and Degraded pass).
  /// </summary>
  public ReadinessThreshold ReadinessThreshold { get; set; } = ReadinessThreshold.Degraded;

  /// <summary>
  /// Number of consecutive evaluations below ReadinessThreshold before
  /// this check actually affects IsReady. Default: 1 (immediate).
  /// </summary>
  public int FailureThreshold { get; set; } = 1;

  /// <summary>
  /// Number of consecutive passing evaluations required to restore
  /// IsPassingForReadiness after a failure. Default: 1 (instant recovery).
  /// Set higher to prevent flapping when a dependency is unstable.
  /// </summary>
  public int SuccessThreshold { get; set; } = 1;
}
```

#### HealthCheckState tracking

```csharp
public sealed class HealthCheckState
{
  public required string Name { get; init; }
  public HealthCheckStatus Status { get; set; } = HealthCheckStatus.Unknown;
  public DateTimeOffset? LastCheckedAt { get; set; }
  public TimeSpan? Duration { get; set; }
  public string? Error { get; set; }

  /// <summary>
  /// Whether this check affects the IsReady computation.
  /// Set from HiveHealthCheckOptions.AffectsReadiness during registration.
  /// </summary>
  public required bool AffectsReadiness { get; init; }

  /// <summary>
  /// The minimum status for this check to be considered passing.
  /// Set from HiveHealthCheckOptions.ReadinessThreshold during registration.
  /// </summary>
  public required ReadinessThreshold ReadinessThreshold { get; init; }

  /// <summary>
  /// Number of consecutive evaluations that fell below the ReadinessThreshold.
  /// Reset to 0 when a passing result is observed.
  /// </summary>
  public int ConsecutiveFailures { get; set; }

  /// <summary>
  /// Number of consecutive passing evaluations since the last failure.
  /// Used with SuccessThreshold to prevent flapping during recovery.
  /// Reset to 0 when a failing result is observed.
  /// </summary>
  public int ConsecutiveSuccesses { get; set; }

  /// <summary>
  /// Whether this check is currently considered passing for readiness,
  /// taking into account ReadinessThreshold, FailureThreshold, and SuccessThreshold.
  /// </summary>
  public bool IsPassingForReadiness { get; set; } = true;
}
```

#### Readiness computation

```
IsReady = IsStarted
  AND all checks where AffectsReadiness=true have IsPassingForReadiness=true
```

Where `IsPassingForReadiness` is computed after each evaluation via pattern matching:

```csharp
bool IsPassing(HealthCheckStatus status, ReadinessThreshold threshold) => threshold switch
{
  ReadinessThreshold.Degraded => status is HealthCheckStatus.Healthy or HealthCheckStatus.Degraded,
  ReadinessThreshold.Healthy => status is HealthCheckStatus.Healthy,
  _ => false
};

// After each evaluation:
if (IsPassing(status, options.ReadinessThreshold))
{
  state.ConsecutiveSuccesses++;
  state.ConsecutiveFailures = 0;

  // SuccessThreshold only applies during recovery (was previously not passing).
  // If already passing, stay passing.
  if (!state.IsPassingForReadiness)
    state.IsPassingForReadiness = state.ConsecutiveSuccesses >= options.SuccessThreshold;
}
else
{
  state.ConsecutiveFailures++;
  state.ConsecutiveSuccesses = 0;
  state.IsPassingForReadiness = state.ConsecutiveFailures < options.FailureThreshold;
}
```

Note: `HealthCheckStatus` and `ReadinessThreshold` are different enums — comparison uses explicit pattern matching, not numeric ordering.

Note: `SuccessThreshold` defaults to 1 (instant recovery). When set to e.g. 3, a check that has failed and tripped `FailureThreshold` must pass 3 consecutive times before `IsPassingForReadiness` is restored. This prevents flapping when a dependency is intermittently available.

#### Thread safety

With independent timers, multiple tasks update state concurrently. `HealthCheckRegistry` uses a `lock` to atomically update a check's state AND recompute `IsReady`:

```csharp
// In HealthCheckRegistry:
private readonly object _sync = new();

void UpdateAndRecompute(string name, HealthCheckStatus status, ...)
{
  lock (_sync)
  {
    // 1. Update this check's state
    state.Status = status;
    state.ConsecutiveFailures = ...;
    state.ConsecutiveSuccesses = ...;
    state.IsPassingForReadiness = ...;

    // 2. Recompute IsReady across all states
    service.IsReady = service.IsStarted
      && _states.Values
        .Where(s => s.AffectsReadiness)
        .All(s => s.IsPassingForReadiness);
  }
}
```

The lock is held for microseconds (in-memory field updates + a LINQ scan over a small collection). No contention in practice — health checks evaluate on 10-60 second intervals. `ReadinessMiddleware` reads `IsReady` (a `ConcurrentDictionary`-backed property) and `GetSnapshots()` (which also acquires the lock to produce a consistent snapshot).

#### Example

```csharp
.WithHealthChecks(checks =>
{
  checks.WithHealthCheck<RabbitMqHealthCheck>(cfg =>
  {
    cfg.AffectsReadiness = true;
    cfg.ReadinessThreshold = ReadinessThreshold.Degraded; // default
    cfg.FailureThreshold = 3; // must fail 3x in a row before affecting readiness
  });
  checks.WithHealthCheck<PaymentGatewayHealthCheck>(cfg =>
  {
    cfg.AffectsReadiness = true;
    cfg.ReadinessThreshold = ReadinessThreshold.Healthy; // strict: Degraded = failing
    cfg.FailureThreshold = 1; // immediate
  });
})
```

#### IConfiguration

```json
{
  "Hive": {
    "HealthChecks": {
      "Checks": {
        "RabbitMq": {
          "ReadinessThreshold": "Degraded",
          "FailureThreshold": 3
        }
      }
    }
  }
}
```

#### Rationale

- **Default `ReadinessThreshold.Degraded`** matches ASP.NET Core convention (`Degraded` → HTTP 200) and prevents cascading outages when all pods see a globally degraded dependency.
- **`FailureThreshold`** absorbs transient blips (e.g., a single slow database query) without the complexity of time-windowed averages. It mirrors the K8s `failureThreshold` concept that operators already understand.
- The combination of `ReadinessThreshold` + `FailureThreshold` gives fine-grained control without boolean proliferation.

---

### ~~Q2: Should health checks run on independent intervals or a single shared sweep?~~ RESOLVED

**Decision:** Independent timers. Each health check runs on its own interval.

**Behavior:**
- Each check gets its own `PeriodicTimer` running at `HiveHealthCheckOptions.Interval` (falls back to `HealthChecksOptions.Interval` if not overridden)
- `IsReady` is recomputed after **each individual check** completes — not batched
- The `HealthCheckBackgroundService` spawns one long-running task per registered check
- All tasks are started after the startup phase completes (blocking checks have already been evaluated once at that point)
- Each task independently: waits for its timer tick → evaluates → updates `HealthCheckState` → recomputes `IsReady`

**Why:** Critical checks (e.g., RabbitMQ broker) can poll every 10s while expensive checks (e.g., full database connectivity) poll every 60s. This avoids penalizing fast checks with the slowest check's cadence.

---

### ~~Q3: What should happen to the existing dead `ConfigureHealthChecks(IHealthChecksBuilder)` on `MicroServiceExtension`?~~ RESOLVED

**Decision:** Remove it. Clean break.

**Actions:**
1. Remove `ConfigureHealthChecks(IHealthChecksBuilder)` from `MicroServiceExtension` base class
2. Remove the override in `MessagingExtensionBase`
3. Remove the `RabbitMqTransportProvider.ConfigureHealthChecks()` implementation
4. Remove `ConfigureHealthChecks` from `IMessagingTransportProvider` interface
5. Replace with `RabbitMqHealthCheck : HiveHealthCheck<RabbitMqHealthCheckOptions>` in `Hive.Messaging.RabbitMq`

**Rationale:** The method was dead code — never wired into the pipeline. Hive is pre-1.0, so breaking changes are acceptable. The new `Hive.HealthChecks` system fully replaces this mechanism.

---

### ~~Q4: Should the initial evaluation run synchronously during startup (blocking readiness)?~~ RESOLVED

**Decision:** Configurable per health check via `HiveHealthCheckOptions.BlockReadinessProbeOnStartup`. Defaults to `true` (safe-by-default).

#### Blocking checks (`BlockReadinessProbeOnStartup = true`, the default)

Blocking checks are evaluated **immediately and synchronously during the startup sequence**, before the service is allowed to accept traffic. This is a hard contract:

- `HealthCheckStartupService` implements `IHostedStartupService`
- During `IHostedStartupService.StartAsync()`, it evaluates **only** the blocking checks — sequentially, one by one
- Each blocking check's `EvaluateAsync()` runs to completion. Its result is written to `HealthCheckState` immediately.
- If **any** blocking check returns `Unhealthy`, the startup sequence fails — `StartupService` catches the exception, signals `StartupFailed`, and the application exits (same as any `IHostedStartupService` failure today)
- Only after **all** blocking checks return `Healthy` or `Degraded` does the `IHostedStartupService` complete, allowing `StartupService` to set `IsReady = true`

This means: **a service with blocking health checks will never report readiness without having verified those dependencies at least once.**

#### Non-blocking checks (`BlockReadinessProbeOnStartup = false`)

Non-blocking checks are **not evaluated during startup**. They:

- Start with `HealthCheckStatus.Unknown`
- Do not prevent `IsReady` from becoming `true`
- Are first evaluated when the background loop begins (after startup completes)

Use this for optional/non-critical dependencies where blocking startup would cause more harm than briefly serving traffic without verification (e.g., a metrics collector, an analytics service).

#### Example

```csharp
.WithHealthChecks(checks =>
{
  checks.WithHealthCheck<RabbitMqHealthCheck>(cfg =>
  {
    cfg.BlockReadinessProbeOnStartup = true;  // default — must verify broker before serving
  });
  checks.WithHealthCheck<MetricsCollectorHealthCheck>(cfg =>
  {
    cfg.BlockReadinessProbeOnStartup = false;  // optional dependency, don't block startup
  });
})
```

#### Startup sequence (detailed)

```
Host starts → ApplicationStarted event fires
    │
    ▼
StartupService.ExecuteHostedStartupServices()
    │
    ├── ... other IHostedStartupService instances ...
    │
    ├── HealthCheckStartupService.StartAsync()
    │       │
    │       ├── Evaluate RabbitMqHealthCheck (blocking=true)
    │       │     → Healthy ✓  (state written immediately)
    │       │
    │       ├── Evaluate DatabaseHealthCheck (blocking=true)
    │       │     → Healthy ✓  (state written immediately)
    │       │
    │       └── Skip MetricsCollectorHealthCheck (blocking=false)
    │             → remains Unknown, not evaluated yet
    │
    ▼
All IHostedStartupService completed successfully
    │
    ├── IsStarted = true
    ├── IsReady = true           ← traffic can now flow
    └── ServiceStarted signaled
    │
    ▼
Background loop starts (evaluates ALL checks at their configured intervals)
    │
    ├── RabbitMqHealthCheck   → re-evaluated every 15s
    ├── DatabaseHealthCheck   → re-evaluated every 60s
    └── MetricsCollectorHealthCheck → first evaluation happens here
```

#### Failure during startup

```
HealthCheckStartupService.StartAsync()
    │
    ├── Evaluate RabbitMqHealthCheck (blocking=true)
    │     → Unhealthy ✗
    │
    ▼
IHostedStartupService throws → StartupService catches
    │
    ├── StartupFailed signaled
    ├── ExitCode = -1
    └── Application stops
```

---

### ~~Q5: Should `Hive.HealthChecks` abstractions live in `Hive.Abstractions` or in their own package?~~ RESOLVED

**Decision:** Thin read-only interface in `Hive.Abstractions`, full runtime in `Hive.HealthChecks`.

#### What goes into `Hive.Abstractions`

These are the **read-only contracts** needed by `ReadinessMiddleware` (in `Hive.MicroServices`) to optionally enrich the readiness response, plus the `IHiveHealthCheck` interface with its static abstract `ConfigureDefaults` method and the `HiveHealthCheckOptions` POCO it references:

```csharp
// Hive.Abstractions/HealthChecks/HealthCheckStatus.cs
public enum HealthCheckStatus
{
  Unknown,
  Healthy,
  Degraded,
  Unhealthy
}

// Hive.Abstractions/HealthChecks/ReadinessThreshold.cs
public enum ReadinessThreshold
{
  Degraded,  // Default — Healthy and Degraded both pass
  Healthy    // Strict — only Healthy passes
}

// Hive.Abstractions/HealthChecks/HealthCheckStateSnapshot.cs
public sealed record HealthCheckStateSnapshot(
  string Name,
  HealthCheckStatus Status,
  DateTimeOffset? LastCheckedAt,
  TimeSpan? Duration,
  string? Error,
  bool AffectsReadiness,
  ReadinessThreshold ReadinessThreshold,
  int ConsecutiveFailures,
  int ConsecutiveSuccesses,
  bool IsPassingForReadiness);

// Hive.Abstractions/HealthChecks/IHealthCheckStateProvider.cs
public interface IHealthCheckStateProvider
{
  IReadOnlyList<HealthCheckStateSnapshot> GetSnapshots();
}

// Hive.Abstractions/HealthChecks/HiveHealthCheckOptions.cs
/// <summary>
/// Per-check configuration. Lives in Abstractions because it is referenced
/// by IHiveHealthCheck.ConfigureDefaults (static abstract).
/// Only uses BCL types and ReadinessThreshold (also in Abstractions).
/// </summary>
public sealed class HiveHealthCheckOptions
{
  public TimeSpan? Interval { get; set; }
  public bool AffectsReadiness { get; set; } = true;
  public bool BlockReadinessProbeOnStartup { get; set; } = true;
  public ReadinessThreshold ReadinessThreshold { get; set; } = ReadinessThreshold.Degraded;
  public int FailureThreshold { get; set; } = 1;
  public int SuccessThreshold { get; set; } = 1;
}

// Hive.Abstractions/HealthChecks/IHiveHealthCheck.cs
/// <summary>
/// Interface for Hive health checks. Implementations are
/// auto-discovered via Scrutor assembly scanning when
/// .WithHealthChecks() is registered.
///
/// Uses C# 11 static abstract members so that ConfigureDefaults
/// can be called during ConfigureServices (before DI container is built)
/// without needing an instance of the health check.
/// </summary>
public interface IHiveHealthCheck
{
  /// <summary>
  /// Instance-level name, used at runtime for probes, logging, and display.
  /// The HiveHealthCheck base class provides a default implementation
  /// that delegates to CheckName via a reflection bridge.
  /// </summary>
  string Name { get; }

  /// <summary>
  /// Static name used during ConfigureServices (before DI container exists)
  /// for IConfiguration section lookup: Hive:HealthChecks:Checks:{CheckName}:...
  /// and for TOptions convention binding.
  /// </summary>
  static abstract string CheckName { get; }

  /// <summary>
  /// Provide check-specific default options (e.g., AffectsReadiness, Interval).
  /// Called as T.ConfigureDefaults(options) during service registration —
  /// no instance required. IConfiguration and fluent API overrides
  /// take precedence over these defaults.
  /// </summary>
  static abstract void ConfigureDefaults(HiveHealthCheckOptions options);
}
```

#### What goes into `Hive.HealthChecks`

Everything else — the full runtime:

- `HiveHealthCheck` abstract base class (for extension authors to inherit)
- `HiveHealthCheck<TOptions>` options-aware variant
- `HealthChecksOptions` global configuration
- `HealthCheckRegistry` (implements `IHealthCheckStateProvider`)
- `HealthCheckStartupService` (`IHostedStartupService` — blocking startup evaluation)
- `HealthCheckBackgroundService` (`BackgroundService` — independent timer loops)
- `HealthChecksExtension` (`MicroServiceExtension<HealthChecksExtension>`)
- `Startup` (extension methods: `WithHealthChecks()`, `WithHealthCheck<T>()`)

#### Dependency flow

```
Hive.Abstractions
  + IHealthCheckStateProvider (interface)
  + HealthCheckStatus (enum)
  + HealthCheckStateSnapshot (record)
  + ReadinessThreshold (enum)
  + HiveHealthCheckOptions (POCO — only BCL types + ReadinessThreshold)
  + IHiveHealthCheck (interface with static abstract ConfigureDefaults)
      │
      ├── Hive.HealthChecks → Hive.Abstractions
      │     (implements IHealthCheckStateProvider, provides HiveHealthCheck base class)
      │     (Scrutor scans for IHiveHealthCheck implementations at startup)
      │
      ├── Hive.MicroServices → Hive.Abstractions
      │     (ReadinessMiddleware resolves IHealthCheckStateProvider? from DI)
      │
      └── Hive.Messaging.RabbitMq → Hive.HealthChecks
            (provides RabbitMqHealthCheck : HiveHealthCheck, auto-discovered)
```

No circular dependencies. `HiveHealthCheckOptions` lives in `Hive.Abstractions` because it's a plain POCO (only BCL types + `ReadinessThreshold` enum) — this allows `IHiveHealthCheck.ConfigureDefaults(HiveHealthCheckOptions)` to be a static abstract method without creating a cross-package type dependency. `Hive.MicroServices` does not reference `Hive.HealthChecks`. Extension authors reference `Hive.HealthChecks` for the `HiveHealthCheck` base class. Scrutor discovers implementations automatically — no manual registration interface needed on extensions.

#### ReadinessMiddleware enrichment

```csharp
// In ReadinessMiddleware.InvokeAsync:
var provider = context.RequestServices.GetService<IHealthCheckStateProvider>();
if (provider is not null)
{
  // Return enriched response with checks array
}
else
{
  // Return current simple response (backward compatible)
}
```

---

### ~~Q6: How should health check failure be handled during evaluation?~~ RESOLVED

**Decision:** Exception = `Unhealthy`. A check that throws is a check that cannot verify its dependency.

**Behavior:**
- Both `HealthCheckStartupService` and `HealthCheckBackgroundService` wrap each `EvaluateAsync()` call in a try-catch
- On exception: status is set to `Unhealthy`, `HealthCheckState.Error` is set to the exception message
- The exception is also logged at `Warning` level (not `Error` — the health check system is handling it by design)
- `ConsecutiveFailures` increments as normal — the exception-caused `Unhealthy` is treated identically to a returned `Unhealthy`

---

### ~~Q7: Should the extension auto-register health checks from other registered extensions?~~ RESOLVED

**Decision:** Auto-discovery via Scrutor assembly scanning for `IHiveHealthCheck` implementations. No manual wiring needed from extension authors.

#### How it works

Health check types are discovered automatically by scanning loaded assemblies for classes implementing `IHiveHealthCheck` (marker interface in `Hive.Abstractions`). This uses Scrutor, which is already a dependency of `Hive.Abstractions`.

```csharp
// In HealthChecksExtension.ConfigureServices():
if (!options.DisableAutoDiscovery)
{
  services.Scan(scan => scan
    .FromApplicationDependencies()
    .AddClasses(classes => classes.AssignableTo<IHiveHealthCheck>())
    .As<IHiveHealthCheck>()
    .WithSingletonLifetime());
}

// Evaluation service resolves all checks via DI:
// IEnumerable<IHiveHealthCheck> checks (constructor-injected)
```

**Implementation note — static abstract invocation on runtime types:**
Scrutor discovers types at runtime (`Type` objects), but `T.CheckName` and `T.ConfigureDefaults(options)` require compile-time generic type parameters. This is bridged via `MakeGenericMethod`:

```csharp
internal static class ReflectionBridge
{
  public static string GetCheckName(Type healthCheckType)
    => (string)typeof(Invoker<>).MakeGenericType(healthCheckType)
      .GetMethod(nameof(Invoker<IHiveHealthCheck>.GetName))!.Invoke(null, null)!;

  public static void InvokeConfigureDefaults(Type healthCheckType, HiveHealthCheckOptions options)
    => typeof(Invoker<>).MakeGenericType(healthCheckType)
      .GetMethod(nameof(Invoker<IHiveHealthCheck>.Configure))!.Invoke(null, [options]);

  private static class Invoker<T> where T : IHiveHealthCheck
  {
    public static string GetName() => T.CheckName;
    public static void Configure(HiveHealthCheckOptions o) => T.ConfigureDefaults(o);
  }
}
```

This bridge is called once per discovered type during `ConfigureServices` — the cost is negligible.

**Extension author work:** create a `HiveHealthCheck` subclass. That's it. No interface to implement on the extension class, no registration record to return.

```csharp
// In Hive.Messaging.RabbitMq — this is ALL that's needed
public sealed class RabbitMqHealthCheck : HiveHealthCheck<RabbitMqHealthCheckOptions>
{
  public static string CheckName => "RabbitMq";
  // Name inherited from base class → "RabbitMq"

  public static void ConfigureDefaults(HiveHealthCheckOptions options)
  {
    options.AffectsReadiness = true;
    options.BlockReadinessProbeOnStartup = true;
  }

  public override async Task<HealthCheckStatus> EvaluateAsync(
    CancellationToken ct)
  {
    // IConnection is constructor-injected
    // Options.ManagementApiUri available via base class
  }
}
```

**Discovery + defaults flow:**
1. Scrutor finds `RabbitMqHealthCheck` (implements `IHiveHealthCheck` via `HiveHealthCheck` base)
2. During `ConfigureServices`, `HealthChecksExtension` reads `T.CheckName` and calls `T.ConfigureDefaults(options)` — both **static abstract**, no instance needed
3. Uses `T.CheckName` to look up `Hive:HealthChecks:Checks:{CheckName}:...` in IConfiguration and applies overrides
4. If user also called `.WithHealthCheck<RabbitMqHealthCheck>(cfg => ...)`, that takes highest precedence

Explicitly registered `.WithHealthCheck<T>(cfg => ...)` calls always take precedence over auto-discovered defaults.

#### Opt-out

```csharp
.WithHealthChecks(checks =>
{
  checks.DisableAutoDiscovery = true; // only use explicitly registered checks
})
```

#### Why Scrutor over IHealthCheckProvider

| Aspect | IHealthCheckProvider (rejected) | Scrutor scan (chosen) |
|--------|-------------------------------|----------------------|
| Extension author work | Implement interface + return registrations | Just create a HiveHealthCheck subclass |
| Cross-package types | `HealthCheckRegistration` in Abstractions → circular dependency with `HiveHealthCheckOptions` | None — `IHiveHealthCheck` marker is self-contained |
| Defaults mechanism | Via `Action<HiveHealthCheckOptions>` delegate (causes the cycle) | Via `IHiveHealthCheck.ConfigureDefaults()` static abstract (options POCO in Abstractions) |
| Existing precedent | IActivitySourceProvider (returns strings, no cross-package types) | Scrutor already in Hive.Abstractions (used for `Decorate<>`) |
| Messaging extension changes | Must implement IHealthCheckProvider | No changes — subclass is discovered automatically |

---

## 5. Integration with Hive.Messaging.RabbitMq

`Hive.Messaging.RabbitMq` will:

1. Add a `<ProjectReference>` to `Hive.HealthChecks`
2. Provide `RabbitMqHealthCheck : HiveHealthCheck<RabbitMqHealthCheckOptions>` that checks broker connectivity
3. Remove (or deprecate) the current `ConfigureHealthChecks(IHealthChecksBuilder)` override in `MessagingExtensionBase`
4. The existing ASP.NET Core `AddRabbitMQ()` health check registration would be replaced

## 6. Dependency Graph (updated)

```
Hive.Abstractions (foundation)
  + IHealthCheckStateProvider, HealthCheckStatus, HealthCheckStateSnapshot
  + ReadinessThreshold, HiveHealthCheckOptions, IHiveHealthCheck (static abstract ConfigureDefaults)
    │
    ├── Hive.HealthChecks → Hive.Abstractions
    │     (HiveHealthCheck base class, HealthCheckRegistry, EvaluationService)
    │     (Scrutor scans for IHiveHealthCheck implementations)
    │
    ├── Hive.MicroServices → Hive.Abstractions (unchanged)
    │     (ReadinessMiddleware resolves IHealthCheckStateProvider? optionally)
    │
    ├── Hive.Messaging → Hive.Abstractions (unchanged, no health check wiring needed)
    │     └── Hive.Messaging.RabbitMq → Hive.Messaging, Hive.HealthChecks
    │           (provides RabbitMqHealthCheck : HiveHealthCheck<RabbitMqHealthCheckOptions> — auto-discovered by Scrutor)
    │
    └── Hive.OpenTelemetry → Hive.Abstractions (unchanged)
```

## 7. Non-Goals (v1)

- Health check UI dashboard
- Push-based notifications (webhooks on state change)
- Circuit breaker integration
- Health check history/retention
- Custom probe endpoints beyond `/status/readiness`
