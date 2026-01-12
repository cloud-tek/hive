# CORS Extraction Analysis - Executive Summary

**Date**: 2026-01-12
**Context**: Evaluating if CORS should be extracted to `Hive.CORS` module for reuse across Hive.MicroServices and Hive.Functions

---

## TL;DR Recommendation

**DON'T extract CORS to a separate module yet.**

Keep CORS embedded in each hosting model (`Hive.MicroServices.CORS`, `Hive.Functions.CORS`) with optional shared abstractions for configuration models only.

---

## Current State

**Location:** [hive.microservices/src/Hive.MicroServices/CORS/](hive.microservices/src/Hive.MicroServices/CORS/)

**Components:**
- ✅ `Options.cs` - Configuration model (100% portable)
- ✅ `CORSPolicy.cs` - Policy model (100% portable)
- ✅ `OptionsValidator.cs` - FluentValidation rules (95% portable)
- ✅ `CORSPolicyValidator.cs` - Policy validation (100% portable)
- ⚠️ `Extension.cs` - Service registration (70% portable, has logger coupling)
- ❌ Middleware application - ASP.NET Core specific (`app.UseCors()`)

**Key Coupling Issue:**
```csharp
// Extension.cs:62, 78, 85
((MicroService)Service).Logger.LogInformationPolicyConfigured(...);
```
This cast prevents clean extraction.

---

## Two Extraction Options

### Option A: Full Extraction (NOT Recommended)

**Structure:**
```
hive.cors/
  ├── src/Hive.CORS/                 # Core config/validation
  ├── adapters/
  │   ├── Hive.CORS.AspNetCore/      # ASP.NET middleware
  │   └── Hive.CORS.Functions/       # Functions middleware
  └── tests/Hive.CORS.Tests/
```

**Why NOT to do this now:**
- ⏱️ Significant refactoring (6+ files, 5+ projects modified)
- 🎯 YAGNI - Only 2 hosting models (MicroServices, Functions)
- 🧪 All CORS integration tests need migration
- 🔀 Abstraction complexity may not provide value yet
- 📦 All pipeline modes (Api, GraphQL, Grpc, Job) need updates

### Option B: Lightweight Abstractions (RECOMMENDED)

**Structure:**
```
hive.microservices/src/Hive.MicroServices/CORS/  # Keep as-is
hive.functions/src/Hive.Functions/CORS/          # New, Functions-specific

Optional (future):
hive.cors.abstractions/                          # Just POCOs
  └── Options.cs, CORSPolicy.cs
```

**Why this is better:**
- ✅ Pragmatic - CORS middleware IS fundamentally different per hosting model
- ✅ Lower risk - Minimal code churn
- ✅ Clear boundaries - Each model owns its implementation
- ✅ Shared config - Same JSON configuration works for both
- ✅ YAGNI compliant - Build abstractions when you need them (3+ models)

---

## Implementation Plan for Hive.Functions

**Step 1: Copy CORS implementation to Functions**
```csharp
// hive.functions/src/Hive.Functions/CORS/Extension.cs
namespace Hive.Functions.CORS;

public class Extension : MicroServiceExtension
{
    public Extension(IFunctionHost functionHost) : base(functionHost) { }

    public override IServiceCollection ConfigureServices(...)
    {
        // Load and validate CORS options (same as MicroServices)
        // Register CORS configuration in DI
    }

    public void ConfigureFunctions(IFunctionsWorkerApplicationBuilder builder)
    {
        // Functions-specific middleware
        builder.Use(async (FunctionContext context, Func<Task> next) =>
        {
            if (context.IsHttpRequest(out var httpReqData))
            {
                ApplyCorsHeaders(httpReqData, context.InstanceServices);
            }
            await next();
        });
    }

    private void ApplyCorsHeaders(HttpRequestData request, ...)
    {
        // Apply CORS headers based on policy configuration
        // Access-Control-Allow-Origin
        // Access-Control-Allow-Methods
        // Access-Control-Allow-Headers
    }
}
```

**Step 2: Verify configuration compatibility**

Same configuration works for both:
```json
{
  "Hive": {
    "CORS": {
      "AllowAny": false,
      "Policies": [
        {
          "Name": "WebApp",
          "AllowedOrigins": ["https://app.example.com"],
          "AllowedMethods": ["GET", "POST"],
          "AllowedHeaders": ["Content-Type", "Authorization"]
        }
      ]
    }
  }
}
```

**Step 3: Add tests**
- Verify CORS headers applied correctly in Functions context
- Test preflight (OPTIONS) requests
- Test origin validation and rejection

---

## Decision Criteria for Future Extraction

**Revisit full extraction (Option A) when:**
- ✅ 3+ hosting models need CORS
- ✅ Code duplication becomes a maintenance burden
- ✅ Validators diverge significantly between implementations
- ✅ Middleware abstraction proves valuable in practice

**Until then:** Keep it simple, embrace duplication for clarity.

---

## Key Insights

### What Makes CORS Hard to Extract?

1. **Middleware is hosting-specific**
   - ASP.NET Core uses `IApplicationBuilder` with `app.UseCors()`
   - Functions use `IFunctionsWorkerApplicationBuilder` with `FunctionContext`
   - No common abstraction exists that doesn't feel forced

2. **Middleware placement matters**
   ```
   UseRouting()
     ↓
   UseCors()      ← MUST be here in ASP.NET Core
     ↓
   UseAuthorization()
     ↓
   UseEndpoints()
   ```
   Functions have different pipeline semantics (per-invocation, not per-application)

3. **Configuration is the same, application is different**
   - Configuration models (Options, CORSPolicy) are 100% portable
   - Validation logic is 95% portable (only uses IMicroService.Environment)
   - Middleware application is 0% portable (completely different APIs)

### What's Actually Reusable?

✅ **Configuration models** - Pure POCOs, no dependencies
✅ **Validation rules** - FluentValidation logic
✅ **Options pattern** - Loading from `Hive:CORS` section
❌ **Middleware registration** - Hosting-specific
❌ **Header application logic** - Different request/response APIs

---

## Related Documentation

- [HIVE_FUNCTIONS_DESIGN.md](HIVE_FUNCTIONS_DESIGN.md) - Full Azure Functions integration design
- [HIVE_FUNCTIONS_DESIGN.md#11](HIVE_FUNCTIONS_DESIGN.md#11-cors-module-extraction-analysis) - Detailed CORS extraction analysis
- [hive.microservices/src/Hive.MicroServices/CORS/README.md](hive.microservices/src/Hive.MicroServices/CORS/README.md) - Current CORS documentation

---

## Action Items

**For MVP (Immediate):**
- [ ] Keep `Hive.MicroServices.CORS` as-is
- [ ] Create `Hive.Functions.CORS` with Functions-specific middleware
- [ ] Document configuration compatibility
- [ ] Add integration tests for Functions CORS

**For Future (Post-MVP):**
- [ ] Monitor code duplication between implementations
- [ ] If duplication becomes painful, extract `Hive.CORS.Abstractions` (just POCOs)
- [ ] If 3rd hosting model emerges, reconsider full extraction

---

**Bottom Line:** CORS is one of those cases where **duplication is better than the wrong abstraction**. The hosting models are fundamentally different, and trying to force them into a shared abstraction adds complexity without proportional value. Keep it simple, keep it clear, keep it maintainable.
