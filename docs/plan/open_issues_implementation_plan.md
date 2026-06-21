# Open Issues — Implementation Plan

**Status:** Approved decisions captured; awaiting sign-off to begin implementation.
**Date:** 2026-06-21
**Version source of truth:** `Version.targets` → currently `10.1.0` (untouched until sign-off)
**Issues covered:** [#62](https://github.com/cloud-tek/hive/issues/62), [#64](https://github.com/cloud-tek/hive/issues/64), [#65](https://github.com/cloud-tek/hive/issues/65), [#66](https://github.com/cloud-tek/hive/issues/66)

## Versioning rule

The single source of truth is `Version.targets` (`<VersionPrefix>`) at the repo root.

- **New features** increment the **minor** version (`10.1.0` → `10.2.0`).
- **Bug fixes** increment the **patch** version (`10.1.0` → `10.1.1`).
- When changes are bundled, the highest-precedence change wins (any feature ⇒ minor bump).

## Triage summary

| Issue | Title | Type | Target version |
|-------|-------|------|----------------|
| #64 (sub-finding) | Null `Logger` NRE masks original startup exception | Bug fix | 10.1.1 |
| #66 | Ambiguous/silent environment resolution (`DOTNET_ENVIRONMENT` vs `ASPNETCORE_ENVIRONMENT`) | Bug fix | 10.1.1 |
| #64 (main) | First-class custom HTTP routes alongside any pipeline mode | Feature | 10.2.0 |
| #65 | Docs: pipeline-mode constraints, MCP usage, discoverability | Docs | 10.2.0 |
| #62 | Adopt `IHostApplicationBuilder` / `WebApplication` host | Feature (large, breaking) | 11.0.0 (separate milestone) |

## Locked decisions

1. **Release shape:** ship a fast `10.1.1` safety patch first (bug fixes), then `10.2.0` (feature + docs).
2. **#66 behavior:** **fail-fast always** on conflicting environment variables (stronger than the patch-safe "warn + opt-in" default — chosen deliberately; can break deployments that currently set both vars inconsistently).
3. **#64 design:** **Option 3** — a general `MapEndpoints` seam usable across all pipeline modes (ADR first).
4. **#62:** its own `11.0.0` milestone with a dedicated ADR; not bundled.

---

## Release 10.1.1 — safety patch (bug fixes → patch bump)

**Version edit:** `10.1.0` → `10.1.1`

### PR-1 · #64 sub-finding — `Logger` null NRE

- **Problem:** `MicroServiceBase.Logger` is `default!` and only assigned via the two-arg ctor. The single-arg ctor leaves it null; catch blocks in `RunAsync`/`StartAsync`/`StopAsync` call `Logger.LogUnhandledException(...)`, throwing `NullReferenceException` and masking the original exception (e.g. `OptionsValidationException`).
- **Fix:** in the `MicroService` ctor, default `Logger` to `NullLogger<IMicroService>.Instance` instead of `default!` when no external logger is supplied (keep `ExternalLogger = false`). Single fix point; the three catch blocks then surface the real exception.
  - `hive.microservices/src/Hive.MicroServices/MicroService.cs` (ctor; catch blocks at lines ~194, ~219, ~237)
- **Test:** single-arg `new MicroService("x")` with a `ValidateOnStart` failure → assert the original `OptionsValidationException` surfaces, not an NRE. `[UnitTest]`.
- **Independent** — no dependencies.

### PR-2 · #66 — environment-variable conflict resolution (fail-fast)

- **Problem:** Hive's `Environment` property (`hive.core/src/Hive.Abstractions/MicroServiceBase.cs:52`) reads **only** `ASPNETCORE_ENVIRONMENT`, lowercases it, and defaults to `"dev"`. The underlying host computes `IHostEnvironment.EnvironmentName` from `DOTNET_ENVIRONMENT` (then `ASPNETCORE_ENVIRONMENT` via `ConfigureWebHostDefaults`). Two silent divergence axes: (a) `DOTNET_ENVIRONMENT` vs `ASPNETCORE_ENVIRONMENT`; (b) Hive's lowercased `"dev"`-defaulting property vs the framework's `EnvironmentName`.
- **Fix:**
  - Add `DotNet.RuntimeEnvironment = "DOTNET_ENVIRONMENT"` to `hive.core/src/Hive.Abstractions/Constants.Environment.cs` (currently only `ASPNETCORE_ENVIRONMENT`).
  - Add an internal resolver that reads `ASPNETCORE_ENVIRONMENT`, `DOTNET_ENVIRONMENT`, and the host's effective `IHostEnvironment.EnvironmentName`. **Throw** at startup (e.g. `ConfigurationException` / `InvalidOperationException`) when both env vars are set to case-insensitively different values. Message must name both variables, their values, and which one Hive honors for config loading.
  - Emit a `[LoggerMessage]` source-generated diagnostic (partial class) before throwing.
  - **Constraint:** additive diagnostic only — must NOT reorder or strip env-var / command-line configuration sources (see `feedback_config_precedence` memory). Env vars + command line stay highest precedence.
- **Deferred (out of scope):** realigning Hive's `Environment` property to also read `DOTNET_ENVIRONMENT` — that is a behavior change (could shift which `appsettings.{env}.json` loads) and belongs in a minor/major, not this patch. Document current behavior as part of #65.
- **Test:** `EnvironmentVariableScope` matrix — only-`ASPNETCORE_ENVIRONMENT` / only-`DOTNET_ENVIRONMENT` / both-equal → start OK; both-conflicting → throws. `[UnitTest]`.
- **Independent** — no dependencies.

---

## Release 10.2.0 — feature + docs (feature wins → minor bump)

**Version edit:** `10.1.1` → `10.2.0`

### PR-3 · #64 main — general `MapEndpoints` seam (Option 3)

- **Problem:** pipeline modes are mutually exclusive (`ValidatePipelineModeNotSet`, set-once `PipelineMode`). Consumers needing auxiliary non-pipeline HTTP routes (control-plane, webhooks) on the same service have only the fragile, undocumented `RegisterExtension` + `Configure(app)` ordering workaround, which works solely because `ConfigureExtensions()` appends `extension.Configure(app, this)` before the mode's routing block.
- **Why Option 3:** every pipeline mode (Api, GraphQL, Grpc, Mcp, Job) independently hand-rolls the same `UseRouting → CORS → UseAuthorization → UseEndpoints(MapX)` block. The Hive-idiomatic fix mirrors the existing `ConfigurePipelineActions` list-of-actions pattern, solves the problem once for all modes, and retires the fragile workaround. Additive and non-breaking (the `PipelineMode` invariant is untouched — routes are orthogonal to mode).
- **ADR first** (architect agent). Record:
  - Ordering of custom routes relative to the mode's own endpoints.
  - Custom routes sit inside the same `UseRouting → CORS → UseAuthorization` envelope as mode endpoints.
  - Behavior in `Job` / `None` modes.
  - Retirement of the `RegisterExtension` + `Configure(app)` workaround.
- **API:**
  - New internal state on `MicroService` (mirrors `ConfigurePipelineActions` at `MicroService.cs:105`):

    ```csharp
    internal List<Action<IEndpointRouteBuilder>> MapEndpointActions { get; } = new();
    ```

  - New public extension in core `Hive.MicroServices` (mode-agnostic):

    ```csharp
    /// <summary>
    /// Registers custom endpoint mappings applied alongside the active pipeline mode's
    /// own endpoints, inside the same routing/authorization envelope. Can be called
    /// multiple times and combined with any Configure*Pipeline mode.
    /// </summary>
    public static IMicroService MapEndpoints(
        this IMicroService microservice,
        Action<IEndpointRouteBuilder> map)
    {
        _ = microservice ?? throw new ArgumentNullException(nameof(microservice));
        _ = map ?? throw new ArgumentNullException(nameof(map));
        ((MicroService)microservice).MapEndpointActions.Add(map);
        return microservice;
    }
    ```

  - Each mode's `UseEndpoints` block drains the list after its own `MapX()`. Extract the duplicated `UseRouting/CORS/UseAuthorization/UseEndpoints` block (Api, GraphQL, Grpc, Mcp, Job) into one shared helper:

    ```csharp
    app.UseEndpoints(endpoints =>
    {
        mapModeEndpoints(endpoints);                     // e.g. endpoints.MapMcp()
        foreach (var action in service.MapEndpointActions)
            action(endpoints);
    });
    ```

- **Consumer usage** (identical regardless of mode; call order is fluent-independent):

  ```csharp
  new MicroService("mcp-demo")
      .WithOpenTelemetry()
      .ConfigureMcpPipeline(b => b.WithTools<WeatherForecastTool>())
      .MapEndpoints(routes =>
        routes.MapPost("/admin/reindex", (IWeatherForecastService svc) => Results.Ok()));

  new MicroService("gql-demo")
      .ConfigureGraphQLPipeline(/* ... */)
      .MapEndpoints(routes => routes.MapPost("/webhooks/stripe", Handler.Stripe));
  ```

- **Demo:** update the MCP demo to show a custom route sharing a DI singleton with a tool.
- **Test:** TestServer integration — MCP + custom `POST /admin/...` both respond and share the same singleton instance; spot-check the same seam against one other mode (GraphQL). `[IntegrationTest]`.

### PR-4 · #65 P0 — core docs

- README "Constraints & Gotchas": one-pipeline-per-service, set-once `PipelineMode`, exact exception text, how to choose a mode.
- XML `<remarks>` on each `Configure*Pipeline` method (Api, Mcp, GraphQL, Grpc, Job, core).
- Document the `MapEndpoints` seam (lands with PR-3).
- Document CORS `IMicroService`-only constraint and OpenTelemetry lambda-override replacement semantics.
- Respect editorconfig (2-space; no trailing final newline on `.cs`). Run `cloudtek-build --target All --skip RunChecks` to confirm no doc-comment analyzer warnings (warnings-as-errors is on).

### PR-5 · #65 P1 — `Hive.MicroServices.Mcp/README.md` (**depends on PR-3**)

- Quickstart; tool registration (`[McpServerToolType]` / `[McpServerTool]`, `WithTools<T>()`, DI injection); transport; coexistence with custom routes via the shipped `MapEndpoints` seam (not the workaround); prompts/resources example.
- Use the HTTP/Refit README as the template.

### PR-6 · #65 P2 — documentation discoverability

- ✅ **Done (pre-work):** reorganized `docs/` — design docs moved to `docs/design/`, plan docs to `docs/plan/`, all filenames lowercased, and existing `.md` links updated.
- Add `docs/README.md` index.
- Cross-link extension READMEs; add first-service quickstart; unified build/CI guide.

---

## Separate milestone — 11.0.0

### #62 · Adopt `IHostApplicationBuilder` / `WebApplication` host

- Rearchitect host construction from `Host.CreateDefaultBuilder(...).ConfigureWebHostDefaults(...)` to `WebApplication.CreateBuilder(args)`; add a `ConfigureBuilder(Action<IHostApplicationBuilder>)` seam to unlock `AddServiceDefaults()` and Aspire client integrations.
- **Breaking** to the testing surface (`ConfigureWebHost(IWebHostBuilder, ...)`, `ExternalHostFactory`), both feeding `Hive.MicroServices.Testing`.
- **Major bump → 11.0.0.** Own ADR (architect agent) + phased plan:
  - **Phase A:** validate whether an additive `ConfigureBuilder` seam is feasible on the current host (likely not before the rewire — flag).
  - **Phase B:** the `WebApplication` rewire; resolve probe-middleware ordering, `AddSharedConfiguration` ordering, shutdown drain, test-args timing, builder variant, sibling-package scope.
  - **Phase C:** rewrite `Hive.MicroServices.Testing` on `WebApplicationFactory`; decide deprecation shim vs clean cut.
- Not bundled with 10.x.

---

## Sequencing

- **10.1.1:** PR-1 and PR-2 are independent — can proceed in parallel.
- **10.2.0:** PR-3 ADR precedes PR-3 code; PR-5 depends on PR-3; PR-4 and PR-6 are independent.
- **11.0.0:** #62 ADR precedes any code; separate milestone.

**Suggested start:** the 10.1.1 patch (PR-1 + PR-2) first (independent, ships safety fixes fastest), with the PR-3 ADR drafted in parallel.
