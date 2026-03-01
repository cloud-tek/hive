## Summary

Remove dead code, redundant middleware, and duplicated patterns identified during a comprehensive codebase review.

### What changed

**Dead code removal**
- Deleted `IHaveRequestLoggingMiddleware` interface and all usage sites (Serilog logging module remnant)
- Deleted `TracingMiddleware` — duplicates OpenTelemetry ASP.NET Core instrumentation and can cause orphaned activities
- Deleted `MiddlewareDiagnosticListener` — never instantiated, uses `Console.WriteLine`
- Deleted empty `IFunctionHostExtensions` placeholder class
- Deleted `CompositeConfigurationException` — never thrown or caught
- Removed `InternalsVisibleTo("Hive.Logging")` for the deleted logging module
- Removed debug artifact `ConfigurePipelineActions.GetType()` no-op call
- Removed dead `IHaveRequestLoggingMiddleware` checks in `MicroService.Configure()` and `ConfigureWebHost()`

**Shared configuration factory**
- Extracted `ConfigurationBuilderFactory` in `Hive.Abstractions` with `CreateDefault()` and `AddSharedConfiguration()` extension
- Standardized shared config file name to `appsettings.shared.json` across `MicroService` and `FunctionHost`
- Fixed `optional: true` vs `optional: false` inconsistency for `appsettings.json` in `MicroService.CreateHostBuilder`

**Validation refactoring**
- Refactored `ServiceCollectionExtensions.PreConfiguration` using strategy/delegate pattern
- Extracted `PreConfigureValidatedOptionsCore` and `PreConfigureOptionalValidatedOptionsCore` private methods
- Extracted `ValidateWithDataAnnotations` and `ValidateWithFluentValidation` strategy methods
- Replaced 6 duplicated `switch (errors.Count)` blocks with a single `ThrowValidationErrors` helper

**Other improvements**
- Updated `UseDefaultLoggingConfiguration()` from stale `Hive:Logging:Level` to standard `Logging:LogLevel:Default`
- Hardened `Serialization.JsonOptions.DefaultIndented` with `MakeReadOnly()` and property wrapper to prevent mutation

### Why

A principal-level codebase review identified dead code from the Serilog→OpenTelemetry migration, redundant tracing middleware, duplicated configuration loading patterns, and repeated validation error formatting. These changes reduce maintenance burden and improve code clarity without changing any runtime behavior.

### Impact

| Metric | Value |
|--------|-------|
| Files deleted | 6 |
| Files modified | 8 |
| Files created | 1 (`ConfigurationBuilderFactory.cs`) |
| Net lines | -236 (190 added / 426 removed) |

## Test plan

- [x] `dotnet build Hive.sln` — 0 errors
- [x] `dotnet test Hive.sln --filter "Category=UnitTests|Category=ModuleTests"` — 348 tests pass, 0 failures
- [x] PreConfiguration tests (DataAnnotations, Delegate, FluentValidation, Optional) all pass with refactored validation
- [x] MicroService startup/pipeline tests pass without TracingMiddleware
- [x] FunctionHost tests pass with standardized config file names