# PR-1 Handoff — Logger null NRE fix (#64 sub-finding)

**Branch:** `bug/64`
**Issue:** [#64](https://github.com/cloud-tek/hive/issues/64) (sub-finding)
**Release:** 10.1.1 (safety patch — first of two PRs; PR-2 is #66)
**Type:** Bug fix → patch bump
**Plan reference:** [open_issues_implementation_plan.md](open_issues_implementation_plan.md) → "Release 10.1.1 · PR-1"
**Status:** Implemented & verified; ready for review.

## Problem

`MicroServiceBase.Logger` was initialized to `default!` (null) and only assigned a real
value by the two-arg ctor `MicroService(string, ILogger)` when an external logger was
supplied. The single-arg ctor `MicroService(string)` left it null, and `Logger` is never
reassigned from DI anywhere.

The catch blocks in `RunAsync`, `StartAsync` and `StopAsync` call
`Logger.LogUnhandledException(Name, ex)`. With a null `Logger` this threw a
`NullReferenceException` that **masked the original startup exception** (e.g.
`ConfigurationException`, `OptionsValidationException` from `ValidateOnStart`). The bug
escaped existing coverage because every existing startup test used the two-arg ctor.

## Fix

Single source of truth: defaulted the base-class `Logger` property to a real no-op logger
instead of `default!`. All three catch blocks become safe without being modified, and the
fix also covers any other host deriving from `MicroServiceBase` (e.g. `FunctionHost`).

`ExternalLogger` semantics are unchanged — it stays `false` when no external logger is
supplied (`NullLogger` is not an external logger).

## Files changed

| File | Change |
|------|--------|
| `hive.core/src/Hive.Abstractions/MicroServiceBase.cs` | `Logger` initializer `default!` → `NullLogger<IMicroService>.Instance`; added `using Microsoft.Extensions.Logging.Abstractions;`. Catch blocks untouched. |
| `Version.targets` | `VersionPrefix` `10.1.0` → `10.1.1` (bug fix ⇒ patch). |
| `hive.microservices/tests/Hive.MicroServices.Tests/MicroServiceTests.Startup.cs` | Added two `[UnitTest]` tests using the single-arg ctor; added `using Hive.Exceptions;`. |

## Tests added

Both use the single-arg ctor `new MicroService(ServiceName)` with no pipeline configured
(`PipelineMode.NotSet` ⇒ `ConfigurationException` thrown inside the `try`):

- `GivenSingleArgCtorAndNoPipelineConfigured_WhenRunAsyncIsInvoked_ThenReturnsMinusOneWithoutNullReferenceException`
  — asserts `RunAsync` returns `-1` (previously threw NRE).
- `GivenSingleArgCtorAndNoPipelineConfigured_WhenStartAsyncIsInvoked_ThenThrowsConfigurationException`
  — asserts `StartAsync` rethrows the original `ConfigurationException`, not an NRE.

## Verification

- Build: clean — 0 warnings, 0 errors (`TreatWarningsAsErrors=true`).
- Tests: `--filter FullyQualifiedName~MicroServiceTests.Startup` → **11 passed, 0 failed**
  (9 pre-existing + 2 new).

## CI / dependency note

The initial CI run failed in `cloudtek-build`'s `OutdatedCheck` (not the code) — a
time-based check that fails when newer NuGet versions exist upstream. `main` would fail it
too today; new upstream releases landed after the last green build. Format/style/analyzer
checks all passed. Decompiling `cloudtek-build` confirmed `OutdatedCheck` has no
per-package exclusion (pinning only exempts the *beta* check), so the outdated packages
were updated to restore CI:

| Package(s) | From | To |
|------------|------|----|
| `Microsoft.AspNetCore.MiddlewareAnalysis`, `Mvc.Testing`, `TestHost` + the `Microsoft.Extensions.*` 10.0.x family | 10.0.8 | 10.0.9 |
| `ModelContextProtocol.AspNetCore` | 1.3.0 | 1.4.0 |
| `HotChocolate.AspNetCore` | 16.0.* | 16.2.* (16.2.2) |
| `Refit`, `Refit.HttpClientFactory` | 10.1.6 | 11.2.0 (major) |

Verified: full solution build succeeds; `Hive.HTTP.Tests` 15/15 (Refit 11 OK);
`Hive.MicroServices.Tests` 45/45; `dotnet-outdated` reports 0 outdated under the CI filter.
Pre-existing, unrelated: a High-severity transitive `MessagePack` advisory in the Aspire
**demo** project (not addressed here).

## Notes / scope boundaries

- The catch blocks were intentionally left unchanged — the base default makes them safe.
- The RunAsync path logs to `NullLogger` (silent) then returns `-1` by existing design;
  the StartAsync/StopAsync paths rethrow, so the original exception surfaces there.
- Out of scope (deferred, see plan): realigning `Environment` resolution — that is PR-2
  (#66) in the same 10.1.1 release.
