# Custom Endpoints Seam — `MapEndpoints` Design

**Date**: 2026-06-21
**Status**: Accepted — design gate for PR-3 of the 10.2.0 milestone; both open questions resolved by the human (see [§10](#10-resolved-decisions))
**Issue**: [#64 "main"](https://github.com/cloud-tek/hive/issues/64) — general `MapEndpoints` seam ("Option 3")
**Version impact**: minor bump to **10.2.0** (currently `Version.targets` = `10.1.1`). This document does **not** change `Version.targets`.

---

## TL;DR Recommendation

Add a mode-agnostic seam:

```csharp
public static IMicroService MapEndpoints(
  this IMicroService microservice,
  Action<IEndpointRouteBuilder> map);
```

backed by new internal state on `MicroService`:

```csharp
internal List<Action<IEndpointRouteBuilder>> MapEndpointActions { get; } = new();
```

Each mode's `UseEndpoints(...)` block **drains `MapEndpointActions` immediately after mapping its own endpoints**, so custom routes ride inside the **same** `UseRouting → (UseCors) → UseAuthorization → UseEndpoints` envelope as the mode's endpoints. The change is **additive and non-breaking**; the `PipelineMode` set-once invariant is untouched (routes are orthogonal to mode).

**Two human decisions are now locked (see [§10](#10-resolved-decisions)):**

- **Job services FORBID custom HTTP routes.** `MapEndpoints` stays a pure recorder, but the **Job pipeline-build delegate throws** `ConfigurationException` if `MapEndpointActions` is non-empty when the Job pipeline is constructed. This trades the recorder's "always works on any mode" guarantee — for Job specifically — for a deterministic startup-time failure. The guard keys off the **Job pipeline-build path**, not the `None` enum value.
- **Bare `None` (`ConfigureDefaultServicePipeline`) DRAINS and serves custom routes**, symmetric with the HTTP modes. Note the deliberate asymmetry: both Job and Default resolve to `PipelineMode.None`, yet Default serves custom routes and Job rejects them. The distinction is **Job intent, expressed at the Job pipeline-build site**, not the mode enum.

On the shared-helper question: **do the lighter-touch per-mode drain now, not the full envelope extraction.** Per-mode asymmetries (Api-minimal's Swagger dev pipeline routed through a private `ConfigureApiPipelineInternal`, gRPC's catch-all `MapGet("/")`, Job/None's placeholder route) mean a single shared envelope helper would have to grow parameters to re-introduce exactly the per-mode variation it tries to remove. A one-line drain call in each existing `UseEndpoints` block delivers the seam with far less churn and risk. Full extraction is recorded as **deferred** (see [§9](#9-deferred--out-of-scope)).

---

## 1. Problem

Hive's pipeline modes are mutually exclusive. `MicroService.PipelineMode` is `set-once`, guarded by `ValidatePipelineModeNotSet()`:

```csharp
// hive.microservices/src/Hive.MicroServices/Extensions/IMicroServiceExtensions.cs:134
internal static IMicroService ValidatePipelineModeNotSet(this IMicroService microservice)
{
  var service = (MicroService)microservice;
  if (service.PipelineMode != MicroServicePipelineMode.NotSet)
  {
    throw new InvalidOperationException($"MicroService {nameof(service.PipelineMode)} is already set");
  }
  return microservice;
}
```

A service that selects one mode (`Api`, `ApiControllers`, `GraphQL`, `Grpc`, `Mcp`) has **no first-class way to ALSO expose auxiliary HTTP routes** — control-plane endpoints, webhooks, admin actions — on the same service.

The only current workaround is a fragile, undocumented ordering trick: register a custom `MicroServiceExtension` and use its `Configure(app)` override to call `app.UseEndpoints(...)` (or map routes directly on `app`). This happens to work **only** because `ConfigureExtensions()` appends `extension.Configure(app, this)` to `ConfigurePipelineActions` *before* the mode's routing block is appended:

```csharp
// hive.microservices/src/Hive.MicroServices/MicroService.InternalExtensions.cs:12
internal MicroService ConfigureExtensions()
{
  Extensions.ForEach(extension =>
  {
    ConfigureActions.Add((services, configuration) => extension.ConfigureServices(services, this));
    ConfigurePipelineActions.Add((app) => extension.Configure(app, this));   // appended BEFORE the mode's routing block
  });
  return this;
}
```

That ordering is incidental, not contractual. It places custom routes in a **separate** routing pass outside the mode's `UseRouting → CORS → UseAuthorization` envelope, which means CORS and authorization may not apply consistently. It is exactly the kind of seam that should be promoted to a first-class, documented API.

---

## 2. Grounding in the actual code

Every HTTP-capable path independently hand-rolls the same envelope via `service.ConfigurePipelineActions.Add(app => { ... })`. The canonical clean example is MCP:

```csharp
// hive.microservices/src/Hive.MicroServices.Mcp/IMicroServiceExtensions.cs:38-58
service
    .ConfigureExtensions()
    .ConfigurePipelineActions.Add(app =>
{
  app.UseRouting();

  var corsExtension = microservice.Extensions.SingleOrDefault(x => x is CORS.Extension);
  if (corsExtension is not null)
  {
    app.UseCors();
  }

  app.UseAuthorization();
  app.UseEndpoints(endpoints =>
    {
      endpoints.MapMcp();
    });
});

service.PipelineMode = MicroServicePipelineMode.Mcp;
```

The exhaustive list of consumers (verified via grep over `ConfigurePipelineActions` / `ValidatePipelineModeNotSet` / `PipelineMode` setters):

| Site | File | `UseEndpoints` body | `PipelineMode` set | HTTP? |
|---|---|---|---|---|
| Api minimal | `Hive.MicroServices.Api/IMicroServiceExtensions.cs:50` (`ConfigureApiPipelineInternal`) | user `endpointBuilder` | `Api` (set by caller, **after** internal) | yes |
| ApiControllers | same internal | `endpoints.MapControllers()` | `ApiControllers` (after internal) | yes |
| GraphQL | `Hive.MicroServices.GraphQL/IMicroServiceExtensions.cs:45` | `endpoints.MapGraphQL("/graphql")` | `GraphQL` | yes |
| gRPC / code-first gRPC | `Hive.MicroServices.Grpc/IMicroServiceExtensions.cs:36` | user builder **plus** catch-all `MapGet("/")` | `Grpc` | yes |
| MCP | `Hive.MicroServices.Mcp/IMicroServiceExtensions.cs:38` | `endpoints.MapMcp()` | `Mcp` | yes |
| Job | `Hive.MicroServices.Job/IMicroServiceExtensions.cs:34` | placeholder `MapGet("/")` | **`None`** | **yes** |
| Default pipeline | `Hive.MicroServices/Extensions/IMicroServiceExtensions.cs:151` (`ConfigureDefaultServicePipeline`) | `MapGet("/*")` → 404 | **`None`** | **yes** |

### Asymmetries that matter

1. **Api-minimal vs the rest.** `ConfigureApiPipeline` and `ConfigureApiControllerPipeline` both delegate to a private `ConfigureApiPipelineInternal`, which also wires a **development-only Swagger pipeline** via `UseCoreMicroServicePipeline(developmentOnlyPipeline: ...)`. The `PipelineMode` is set by the **public** method *after* the internal runs — the only mode where the assignment is split from the envelope construction. The envelope shape itself is identical to the others.

2. **`None` is not "non-HTTP".** This is the biggest divergence from the plan's framing. **Job sets `PipelineMode.None` yet builds a full HTTP envelope** (`UseRouting → CORS → UseAuthorization → UseEndpoints`) and serves a placeholder `/`. `ConfigureDefaultServicePipeline` *also* sets `None` and serves a 404 catch-all. So `None` denotes "no *application* request-processing mode," **not** "no HTTP host." Every shipped path that sets a mode — including Job and Default — has a live `UseEndpoints` block.

3. **gRPC** appends its own catch-all `MapGet("/")` *after* the user builder inside the same `UseEndpoints`. Any drained custom routes must be ordered carefully relative to that catch-all (see [§4.1](#41-decision-1--ordering)).

4. **CORS is conditional and `IMicroService`-only.** Every site repeats the identical `SingleOrDefault(x => x is CORS.Extension)` probe and calls `app.UseCors()` only when the extension is present. `CORS.Extension.Create` throws if the host is not an `IMicroService`. CORS application is therefore already a per-mode conditional, not a guaranteed envelope stage.

5. **The `(MicroService)microservice` cast is the established idiom.** Every mode extension, and core helpers like `UseCoreMicroServicePipeline` and `ValidatePipelineModeNotSet`, cast `IMicroService → MicroService` to reach internal state. The plan's sketch is consistent with the codebase. Mode assemblies already have `InternalsVisibleTo` (see [§6](#6-internalsvisibleto--testability)), so they can read the new `MapEndpointActions` directly.

---

## 3. Options considered

### Option A — Lighter-touch per-mode drain (RECOMMENDED)

Add `MapEndpoints` + `MapEndpointActions`. In each existing `UseEndpoints(...)` body, after the mode maps its own endpoints, drain the list:

```csharp
app.UseEndpoints(endpoints =>
{
  endpoints.MapMcp();                                  // mode endpoints first
  foreach (var map in service.MapEndpointActions)      // then custom routes
  {
    map(endpoints);
  }
});
```

- **Pros**: minimal churn (one loop per site); preserves each mode's exact existing behavior including the Api-minimal split, gRPC catch-all ordering, and conditional CORS; trivially reviewable; KISS/YAGNI-aligned.
- **Cons**: the drain line is repeated across ~6 sites (mitigated by a tiny `endpoints.DrainCustomEndpoints(service)` internal helper if desired — still no envelope extraction).

### Option B — Full shared-envelope helper extraction

Extract the duplicated `UseRouting → CORS → UseAuthorization → UseEndpoints` block into one helper taking a `mapModeEndpoints` delegate, then draining `MapEndpointActions`:

```csharp
service.UseModePipeline(mapModeEndpoints: endpoints => endpoints.MapMcp());
```

- **Pros**: removes the 6× envelope duplication; single place for CORS-probe logic.
- **Cons**: to faithfully reproduce per-mode behavior the helper must re-grow parameters — Api-minimal's dev-Swagger pipeline + split mode assignment, gRPC's post-builder catch-all, the placeholder routes in Job/Default. The "shared" helper ends up parameterized back into per-mode variation, defeating the simplification. Higher blast radius across 4+ packages for a PR whose actual feature is the seam, not the refactor. **The plan's sketch under-models these asymmetries** — verified against real code, the clean single-delegate helper it implies does not hold.

### Option C — Promote the extension `Configure(app)` workaround to documented API

Keep the incidental ordering trick but document it.

- **Cons**: routes stay **outside** the mode envelope (separate `UseRouting` pass), so CORS/authorization apply inconsistently — exactly the defect motivating the issue. Rejected.

**Decision: Option A.** Record Option B as deferred.

---

## 4. Required decisions

### 4.1 Decision 1 — Ordering

**Custom routes are drained AFTER the mode maps its own endpoints (mode-endpoints-first).**

Justification:

- **Predictability.** The mode's contract (e.g. `/graphql`, `MapMcp`'s transport routes, `MapControllers`' attribute routes) is the service's primary surface and should win registration order.
- **Route-conflict semantics.** ASP.NET Core endpoint routing resolves conflicts by route **precedence/specificity**, not registration order, so for *distinct* routes order is behaviorally irrelevant. Where it *does* matter is **gRPC's catch-all `MapGet("/")`**: that catch-all must remain the last fallback. Draining custom routes *before* the gRPC catch-all (i.e. mode-user-endpoints → custom → catch-all) keeps the catch-all as the genuine fallback. The recommended placement is "after the mode's *primary* mappings, before any mode-supplied catch-all/fallback."
- **Least surprise on duplicate routes.** If a user maps a route that collides with a mode route, ASP.NET throws an `AmbiguousMatchException` at request time regardless of order — we do not silently shadow. This is acceptable and documented.

Concretely for gRPC the drain goes between the user builder and the catch-all:

```csharp
app.UseEndpoints(endpoints =>
{
  endpointsBuilder(endpoints);                         // mode/user endpoints
  foreach (var map in service.MapEndpointActions) map(endpoints);  // custom
  endpoints.MapGet("/", () => "...gRPC client...");    // fallback stays last
});
```

### 4.2 Decision 2 — Same routing + CORS + UseAuthorization envelope

**Confirmed and intended: custom routes sit inside the same `UseRouting → (UseCors) → UseAuthorization → UseEndpoints` envelope as the mode endpoints.**

Implications and the tradeoff, called out explicitly:

- **Authorization applies.** Custom routes pass through `app.UseAuthorization()`. A custom endpoint that declares `.RequireAuthorization()` is enforced; one that does not is anonymous — identical to mode endpoints. This is **desirable**: it means an admin/control-plane route is secured by the same mechanism, not a side-channel.
- **CORS applies *conditionally*.** `UseCors()` runs only when the CORS extension is present, using the default policy. So custom routes inherit the service's default CORS policy. This is the **right default** (consistency), but the tradeoff is that a custom webhook endpoint that should be CORS-exempt cannot opt out via this seam — it would need per-endpoint CORS metadata. We accept this; per-endpoint CORS override is **out of scope** for PR-3 (see [§9](#9-deferred--out-of-scope)).
- **Single routing pass.** Because the drain is inside the mode's existing `UseEndpoints`, there is no second `UseRouting` and no middleware-ordering ambiguity — this is the concrete improvement over the `Configure(app)` workaround.

### 4.3 Decision 3 — Behavior in non-application modes (`None` / `NotSet`, and Job)

This decision is reshaped by the finding that **`None` already serves HTTP** (Job and `ConfigureDefaultServicePipeline` both set `None` and both have a live `UseEndpoints`).

Chosen behavior:

- **`MapEndpoints` is a pure recorder. It never throws and never inspects `PipelineMode`.** It only appends to `MapEndpointActions`. This keeps the **recording** order-independent: a user may call `MapEndpoints` before or after the mode-selecting call. Enforcement (for Job) is moved downstream to pipeline-build time precisely so that `MapEndpoints` itself can stay a simple, order-independent recorder (see the Job rule below).
- **HTTP application modes (`Api`, `ApiControllers`, `GraphQL`, `Grpc`, `Mcp`) drain and serve custom routes** inside their envelope.
- **Default `None` (`ConfigureDefaultServicePipeline`) drains and serves custom routes** — symmetric with the HTTP modes. `MapEndpoints` works on a bare service; custom routes are served alongside the 404 catch-all. **(Locked decision 2.)**
- **Job (`ConfigureJob`) FORBIDS custom HTTP routes. (Locked decision 1.)** Product intent: worker services must not serve custom HTTP. The Job pipeline does **not** drain `MapEndpointActions`; instead it **throws** when custom routes have been recorded — see "Job enforcement mechanism" below.
- **`NotSet` (no pipeline selected):** `RunAsync`/`StartAsync` already throw `ConfigurationException(Constants.Errors.PipelineNotSet)` when `PipelineMode == NotSet`. A `MapEndpoints` call with no mode selected records actions that are never drained, and the service fails to start for the **existing** reason (no pipeline). We do **not** add a second failure mode for `NotSet`. No new exception, no silent success.

#### Job enforcement mechanism (Locked decision 1)

The human chose **"Forbid on Job"**, overriding the prior "Allow" recommendation. This breaks the recorder's "always works on any mode" property *for Job specifically*: we deliberately replace it with a **deterministic startup-time failure**. The mechanism is specified precisely and honestly:

- **Enforcement happens at Job pipeline-build/drain time, NOT at `MapEndpoints()` call time.** `MapEndpoints` is fluent and call-order-independent — a user may legitimately call `.MapEndpoints(...)` **before** `.ConfigureJob(...)`, at which point `PipelineMode` is still `NotSet`. Validating inside `MapEndpoints` would therefore be unreliable (it would miss the before-`ConfigureJob` ordering and could not distinguish Job from Default, both of which are `None`). So `MapEndpoints` stays a recorder, and the **Job pipeline-build delegate** performs the check once, after all fluent calls have run.

- **Guard call-site:** inside the `ConfigurePipelineActions.Add(app => { ... })` delegate registered by `ConfigureJob` in `Hive.MicroServices.Job/IMicroServiceExtensions.cs` (currently lines 36–52). This delegate is replayed at pipeline-build time (`Host` build during `InitializeAsync`/`RunAsync`, and `ConfigureWebHost` in tests), which is **after** every fluent `MapEndpoints` call has recorded. The guard is the **first statement** of that delegate body — before `app.UseRouting()` — so the failure is raised before any HTTP envelope is constructed:

  ```csharp
  // Hive.MicroServices.Job/IMicroServiceExtensions.cs — inside the ConfigurePipelineActions delegate
  .ConfigurePipelineActions.Add(app =>
  {
    if (service.MapEndpointActions.Count > 0)
    {
      throw new ConfigurationException(
        "Hive.MicroServices.Job (worker) services cannot expose custom HTTP endpoints via MapEndpoints. " +
        "Remove the MapEndpoints call, or select an HTTP pipeline mode (e.g. ConfigureApiPipeline / ConfigureGraphQLPipeline).");
    }

    app.UseRouting();
    // ... existing CORS / UseAuthorization / UseEndpoints placeholder unchanged ...
  });
  ```

- **Exception type and message.** Use **`ConfigurationException`** (from `Hive.Exceptions`, `hive.core/src/Hive.Abstractions/Exceptions/ConfigurationException.cs`) — consistent with how the codebase already signals misconfiguration (`Constants.Errors.PipelineNotSet` is thrown as `ConfigurationException` in `MicroService.RunAsync`/`StartAsync`). Message (exact):
  > `Hive.MicroServices.Job (worker) services cannot expose custom HTTP endpoints via MapEndpoints. Remove the MapEndpoints call, or select an HTTP pipeline mode (e.g. ConfigureApiPipeline / ConfigureGraphQLPipeline).`

  *(Not `InvalidOperationException`: `ConfigurationException` is the repo's established misconfiguration signal and is caught/logged by the same `RunAsync`/`StartAsync` try/catch as the pipeline-not-set case, giving consistent operator-facing behaviour.)*

- **The guard keys off the Job pipeline-build path, NOT the `None` enum.** This is the crux of the Job-vs-Default asymmetry: both `ConfigureJob` and `ConfigureDefaultServicePipeline` set `PipelineMode.None`, so a mode-enum check could not tell them apart. Placing the throw **inside the `ConfigureJob` delegate specifically** (and draining inside the `ConfigureDefaultServicePipeline` delegate specifically) makes Job reject and Default serve, even though both report `None`. The discriminator is *which pipeline-build delegate ran*, i.e. Job intent — never `PipelineMode == None`.

- **Honest tradeoff.** This sacrifices, for Job only, the call-order-independent **"always works"** guarantee that the recorder gives every other mode. In exchange, a Job service that wrongly calls `MapEndpoints` fails **deterministically at startup** with a clear, actionable message, rather than silently serving (the rejected "Allow") or silently dropping routes. The *recording* remains order-independent; only the *outcome* for Job is a startup error. Default-`None` retains the full "always works" guarantee.

### 4.4 Decision 4 — Retirement of the `RegisterExtension` + `Configure(app)` workaround

- **Nothing breaks.** The workaround relies on `MicroServiceExtension.Configure(app, this)` continuing to run, which it does — `ConfigureExtensions()` is unchanged. Existing services using the trick keep working.
- **Deprecate by documentation, not by `[Obsolete]`.** `Configure(app)` is a legitimate extension hook for *middleware* (its real purpose); only its *abuse for endpoint mapping* is being superseded. We therefore do **not** mark anything `[Obsolete]` (that would wrongly flag valid middleware usage and trip `TreatWarningsAsErrors`). Instead:
  - Document `MapEndpoints` as the supported way to add auxiliary routes (README + XML docs).
  - Add a short note in the extension authoring docs: "to add HTTP routes to a moded service, use `MapEndpoints`, not `Configure(app)` + `UseEndpoints`."
- **No migration required** for existing users; the workaround degrades to "still functional but discouraged."

---

## 5. Public API shape & null-guarding

```csharp
namespace Hive.MicroServices;   // core package — available to all modes

public static class IMicroServiceMapEndpointsExtensions
{
  /// <summary>
  /// Registers auxiliary HTTP routes that are served inside the selected pipeline
  /// mode's routing/CORS/authorization envelope, alongside the mode's own endpoints.
  /// Mode-agnostic and additive; does not affect <see cref="MicroServicePipelineMode"/>.
  /// </summary>
  public static IMicroService MapEndpoints(
    this IMicroService microservice,
    Action<IEndpointRouteBuilder> map)
  {
    _ = microservice ?? throw new ArgumentNullException(nameof(microservice));
    _ = map ?? throw new ArgumentNullException(nameof(map));

    var service = (MicroService)microservice;
    service.MapEndpointActions.Add(map);
    return microservice;
  }
}
```

- Lives in **core `Hive.MicroServices`** so a service can call it regardless of which mode package is referenced, and so the state lives next to `ConfigurePipelineActions`.
- The `(MicroService)microservice` cast matches every other core extension (§2 finding 5).
- Null-guards both arguments, consistent with `ConfigureMcpPipeline` / `ConfigureGrpcPipelineInternal`.
- **`MapEndpoints` is a pure recorder and does NOT inspect `PipelineMode` or throw on Job.** The Job prohibition (Locked decision 1, §4.3) is enforced **downstream** in the Job pipeline-build delegate, not here — because `MapEndpoints` may be called before `ConfigureJob`, when the mode is still `NotSet`, and because Job and Default are indistinguishable by the `None` enum at this layer. Keeping `MapEndpoints` a recorder preserves call-order-independence of the recording; see §4.3 "Job enforcement mechanism".
- `MapEndpointActions` declared on `MicroService` next to `ConfigurePipelineActions`:

  ```csharp
  // MicroService.cs, alongside line ~105
  internal List<Action<IEndpointRouteBuilder>> MapEndpointActions { get; } = new();
  ```

---

## 6. InternalsVisibleTo & testability

`MicroService` already exposes internals to every mode package and both test/testing assemblies:

```
Hive.MicroServices.Api, .Grpc, .GraphQL, .Mcp, .Job, .Testing, .Tests
```

So:

- Each mode's drain loop can read `service.MapEndpointActions` directly — **no new `InternalsVisibleTo` entries required.**
- `Hive.MicroServices.Tests` can assert on `MapEndpointActions.Count` for a pure unit test of the recorder, and can exercise the full seam through `ConfigureWebHost` / `ConfigureTestHost` + `TestServer` for integration (the `ConfigureWebHost` helper at `Extensions/IMicroServiceExtensions.cs:82` already replays `ConfigurePipelineActions`, so drained routes are exercised end-to-end).

---

## 7. Test strategy for PR-3

Mirror the existing `MicroServiceTests.CORS.Integration` TestServer pattern (`ConfigureTestHost()` → `Host.GetTestServer()` → `CreateClient()`).

1. **Unit — recorder.** `[UnitTest]`: `new MicroService(...).MapEndpoints(e => ...)` and assert `((MicroService)svc).MapEndpointActions` has one entry; assert null-guards throw `ArgumentNullException`.

2. **Integration — primary (MCP + custom route share DI).** `[IntegrationTest]` using `Hive.MicroServices.Testing`:
   - `ConfigureMcpPipeline(...)` + `MapEndpoints(e => e.MapPost("/admin/flush", (MyState s) => ...))`.
   - Register a **DI singleton** consumed by both the MCP tool and the `/admin/flush` handler; assert both observe the same instance (proves single envelope / shared service provider).
   - Assert the MCP transport endpoint responds **and** `POST /admin/flush` responds `200`.

3. **Integration — cross-mode spot check (GraphQL).** `[IntegrationTest]`: `ConfigureGraphQLPipeline(...)` + `MapEndpoints(e => e.MapGet("/admin/ping", () => Results.Ok()))`; assert `/graphql` and `/admin/ping` both respond. Confirms the seam is genuinely mode-agnostic, not MCP-specific.

4. **Ordering / fallback guard (gRPC).** `[IntegrationTest]`: custom `MapGet("/healthz-custom")` on a gRPC service; assert it resolves and the gRPC catch-all `/` still returns its guidance string (custom drain precedes the catch-all).

5. **Job rejection — startup failure (Locked decision 1).** `[IntegrationTest]` (or `[UnitTest]` driving startup): `new MicroService(...).ConfigureJob().MapEndpoints(e => e.MapGet("/admin/ping", () => Results.Ok()))`, then attempt to start the pipeline (`RunAsync` with a short-cancel, or `ConfigureWebHost` + build). Assert a **`ConfigurationException`** is thrown at pipeline build, with message containing `"Job (worker) services cannot expose custom HTTP endpoints via MapEndpoints"`.
   - **Also assert order-independence of the throw:** a second case calling `.MapEndpoints(...)` **before** `.ConfigureJob()` must throw the *same* exception — proving the guard is at build time, not `MapEndpoints` call time, and is unaffected by call order.
   - Pair with a negative case: `ConfigureJob()` with **no** `MapEndpoints` call starts normally and serves the placeholder `/` (no regression).

6. **Default `None` serves custom routes (Locked decision 2).** `[IntegrationTest]`: `ConfigureDefaultServicePipeline()` + `MapEndpoints(e => e.MapGet("/admin/ping", () => Results.Ok()))`; assert `GET /admin/ping` responds `200` and an unmapped path still returns the `404` catch-all. Confirms Default-`None` drains while Job-`None` throws — the asymmetry keys off the pipeline path, not the enum.

7. *(Optional)* **Auth envelope check.** Custom route with `.RequireAuthorization()` returns `401` unauthenticated, proving it sits inside `UseAuthorization`.

Use `[UnitTest]` / `[IntegrationTest]` trait attributes and `CloudTek.Testing` + `Hive.MicroServices.Testing` per repo conventions.

---

## 8. Implementation task breakdown (PR-3, file by file)

1. **`Hive.MicroServices/MicroService.cs`** — add `internal List<Action<IEndpointRouteBuilder>> MapEndpointActions { get; } = new();` next to `ConfigurePipelineActions` (~line 105). Add `using Microsoft.AspNetCore.Routing;` if not present.
2. **`Hive.MicroServices/` (new file, e.g. `IMicroServiceMapEndpointsExtensions.cs`)** — public `MapEndpoints` extension with null-guards (see §5).
3. **`Hive.MicroServices.Api/IMicroServiceExtensions.cs`** — in `ConfigureApiPipelineInternal`'s `UseEndpoints`, after `endpointBuilder(endpoints)` (note: minimal passes the user delegate; controllers pass `MapControllers`), drain `service.MapEndpointActions`. Covers both `Api` and `ApiControllers`.
4. **`Hive.MicroServices.GraphQL/IMicroServiceExtensions.cs`** — drain after `MapGraphQL("/graphql")`.
5. **`Hive.MicroServices.Grpc/IMicroServiceExtensions.cs`** — drain after `endpointsBuilder(endpoints)` and **before** the catch-all `MapGet("/")`.
6. **`Hive.MicroServices.Mcp/IMicroServiceExtensions.cs`** — drain after `MapMcp()`.
7. **`Hive.MicroServices.Job/IMicroServiceExtensions.cs` — DOES NOT drain; GUARDS instead (Locked decision 1).** Inside the `ConfigurePipelineActions.Add(app => { ... })` delegate (currently lines 36–52), add as the **first statement** (before `app.UseRouting()`): if `service.MapEndpointActions.Count > 0`, `throw new ConfigurationException("Hive.MicroServices.Job (worker) services cannot expose custom HTTP endpoints via MapEndpoints. Remove the MapEndpoints call, or select an HTTP pipeline mode (e.g. ConfigureApiPipeline / ConfigureGraphQLPipeline).");`. Add `using Hive.Exceptions;` if not present. Do **not** add a drain loop to Job, and do **not** call the optional `DrainCustomEndpoints` helper here. The placeholder `MapGet("/")` is unchanged. See §4.3 "Job enforcement mechanism".
8. **`Hive.MicroServices/Extensions/IMicroServiceExtensions.cs` `ConfigureDefaultServicePipeline` — DRAINS (Locked decision 2).** In its `UseEndpoints` body, drain `service.MapEndpointActions` **before** the `MapGet("/*")` 404 catch-all (so custom routes take precedence over the fallback, mirroring the gRPC ordering rule in §4.1). This makes `MapEndpoints` work on a bare `None` service. Note: this is the **Default** `None` path only — it intentionally diverges from the Job `None` path (task 7), and the divergence is expressed by *which delegate* drains vs. throws, not by a `PipelineMode` check.
9. *(Optional helper)* a tiny `internal static void DrainCustomEndpoints(this IEndpointRouteBuilder endpoints, MicroService service)` in core to avoid repeating the loop — keeps each draining site to one line. No envelope extraction. **Used by the draining sites only (Api, GraphQL, gRPC, MCP, Default); Job does not call it** (Job guards, it does not drain).
10. **Tests** — `Hive.MicroServices.Tests`: add the unit + integration files from §7, including the **Job-rejection startup-failure test** (asserting `ConfigurationException` with the expected message, in both call orders) and the **Default-`None`-serves-custom-routes test**.
11. **Docs** — README note for `MapEndpoints`; extension-authoring note discouraging `Configure(app)` for endpoint mapping (§4.4). Link this ADR.
12. **Build/verify** — `dotnet tool run cloudtek-build --target All --skip RunChecks` (then full `All` for the format/analyzer gate; repo runs `TreatWarningsAsErrors` with 2-space indent / file-scoped namespaces / no trailing newline in `.cs`).

**Do not** touch `Version.targets` in PR-3 unless the milestone process bumps it separately; the feature targets 10.2.0 but the version bump is a release concern, not this PR's.

---

## 9. Deferred / out of scope

- **Full shared-envelope helper extraction (Option B).** Deferred; revisit only if a 7th mode lands or the envelope duplication becomes a maintenance burden. The per-mode asymmetries (§2) make the abstraction premature (YAGNI).
- **Per-endpoint CORS opt-out / per-route CORS policy selection** for custom routes (§4.2). Out of scope; custom routes inherit the default policy.
- **A dedicated `Job` enum value** distinct from `None` (§4.3). The `None`-means-two-things ambiguity predates this work and is not introduced by it. **Locked decision 1 now makes this ambiguity load-bearing:** because Job and Default both resolve to `PipelineMode.None`, the Job-forbids / Default-serves split *cannot* be expressed by a mode-enum check and must instead live in the respective pipeline-build delegates (§4.3). A future dedicated `Job` enum value (or a `bool ForbidsCustomEndpoints` flag on `MicroService`) would let the guard be centralised; deferred as a larger change. **Flagged as a NEW consequence of decision 1** — see §11.
- **`MapEndpoints` overloads** (e.g. accepting an `IServiceProvider` or a named-group prefix). YAGNI; ship the single `Action<IEndpointRouteBuilder>` form.

---

## 10. Resolved decisions

Both questions previously open here have been decided by the human. They are now locked into the design above.

1. **Job HTTP exposure (§4.3) — RESOLVED: FORBID on Job.** The human overrode the prior "allow" recommendation. Worker/Job services MUST NOT expose custom HTTP routes via `MapEndpoints`. Enforcement is a **deterministic startup-time failure**, not a `MapEndpoints`-call-time check: the **Job pipeline-build delegate** in `Hive.MicroServices.Job/IMicroServiceExtensions.cs` throws `ConfigurationException` (message in §4.3) as its first statement when `MapEndpointActions` is non-empty. `MapEndpoints` itself stays a pure recorder. This trades the recorder's call-order-independent "always works" guarantee — **for Job only** — for a clear startup error. Captured in: TL;DR, §4.3 (Decision 3 + "Job enforcement mechanism"), §5, §7 (test 5), §8 (task 7, 9, 10).

2. **Default `None` pipeline drain (§8) — RESOLVED: DRAIN.** A bare `ConfigureDefaultServicePipeline` service drains `MapEndpointActions` and serves custom routes (before its 404 catch-all), symmetric with the HTTP modes. Captured in: TL;DR, §4.3 (Decision 3), §8 (task 8), §7 (test 6).

**Job-vs-Default `None` reconciliation (explicit).** Both `ConfigureJob` and `ConfigureDefaultServicePipeline` set `PipelineMode.None`. The forbid/serve split therefore **cannot** key off the enum. It keys off **which pipeline-build delegate runs**: the `ConfigureJob` delegate throws; the `ConfigureDefaultServicePipeline` delegate drains. The discriminator is **Job intent at the build site**, never `PipelineMode == None`. Any future reader changing this must preserve that: do not "simplify" the guard into a `PipelineMode.None` check — it would wrongly reject bare Default services too.

Everything else is determined by the existing code and recorded above.

---

## 11. New issues raised by the locked decisions

- **The `None`-means-two-things ambiguity is now load-bearing (not merely cosmetic).** Before decision 1, `Job == Default == None` was a harmless naming wart. Now the forbid/serve behaviour split rides entirely on the two pipeline-build delegates being distinct; there is no enum-level guard rail. **Risk:** a future refactor that consolidates the Job and Default pipeline-build closures, or that reroutes Job through `ConfigureDefaultServicePipeline`, would silently re-enable custom HTTP on Job (or break Default). **Mitigations already in this design:** (a) the §7 test 5 asserts Job throws in *both* call orders, which fails loudly if the guard is lost; (b) the §4.3 / §10 notes forbid collapsing the guard into a `PipelineMode.None` check. **Recommended follow-up (deferred, §9):** introduce a dedicated `Job` enum value or a `MicroService.ForbidsCustomEndpoints` flag so the guard has a single, enum-anchored home. Not in scope for PR-3; flagged for the milestone owner.

- **No interaction problem found between the Job guard and the rest of the seam.** The guard runs before `app.UseRouting()` in the Job delegate, so it cannot interfere with CORS/auth/endpoint wiring; it shares no state with the draining sites; and because `MapEndpoints` remains a recorder, the gRPC catch-all ordering (§4.1), conditional CORS (§4.2), and the `NotSet` pipeline-not-set guard are all unaffected. The only behavioural change for non-Job modes is the new Default-`None` drain (decision 2), which is additive.

---

## Related documentation

- [cors_extraction_analysis.md](cors_extraction_analysis.md) — CORS placement / `UseCors()` conditional middleware
- [otel_configuration_strategy.md](otel_configuration_strategy.md) — house-style precedent for Options + recommendation + plan
- [../../.claude/rules/architecture.md](../../.claude/rules/architecture.md) — pipeline modes, extension pattern, interface hierarchy
- `hive.microservices/src/Hive.MicroServices.Mcp/IMicroServiceExtensions.cs` — canonical envelope to mirror the drain into
