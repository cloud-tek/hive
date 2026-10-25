# Hive.Functions - Azure Functions Integration Design

> **Status note (issue #71):** the `logging:` / `tracing:` / `metrics:` lambda parameters of `WithOpenTelemetry` shown in this design were not implemented. The shipped API is `WithOpenTelemetry(additionalActivitySources: ...)`, configured via the `OpenTelemetry` configuration section; additional SDK customisation uses `ConfigureServices` + `ConfigureOpenTelemetry{Tracer,Meter,Logger}Provider`. See [hive.opentelemetry/README.md](../../hive.opentelemetry/README.md).

**Status**: Draft Design Document
**Date**: 2026-01-12
**Author**: Architecture Analysis

---

## Executive Summary

This document outlines the design for integrating Azure Functions support into the Hive microservices framework. The implementation will follow Hive's plugin-based architecture while acknowledging that Azure Functions have fundamentally different execution models (event-driven, ephemeral) compared to long-running services (Api, Job, etc.).

### Key Design Principles

1. **Leverage Hive's DI Container** - Reuse service composition, configuration, and extension system
2. **Accept Different Lifecycle** - Functions cold-start behavior differs from IHost lifecycle
3. **Maintain Extension Pattern** - CORS, OpenTelemetry, validation all work via extensions
4. **Embrace Azure Functions SDK** - Don't fight the SDK, adapt Hive to it
5. **Repository Structure Compliance** - Follow established module topology rules

---

## 1. Repository Structure

Following the repository policies in `.claude/rules/repository-policies.md`, create a new module:

```
hive.functions/
  ├── src/
  │   └── Hive.Functions/
  │       ├── Hive.Functions.csproj
  │       ├── IFunctionContext.cs
  │       ├── FunctionHost.cs
  │       ├── FunctionHostBuilder.cs
  │       ├── Extensions/
  │       │   ├── IMicroServiceExtensions.cs
  │       │   └── FunctionExtension.cs
  │       └── Adapters/
  │           ├── HttpRequestAdapter.cs
  │           └── HttpResponseAdapter.cs
  │
  ├── tests/
  │   └── Hive.Functions.Tests/
  │       ├── Hive.Functions.Tests.csproj
  │       └── FunctionHostTests.cs
  │
  └── demo/
      └── Hive.Functions.Demo/
          ├── Hive.Functions.Demo.csproj
          ├── Program.cs
          ├── Functions/
          │   ├── HttpTriggerFunction.cs
          │   └── TimerTriggerFunction.cs
          └── host.json
```

### Module Dependencies

```
Hive.Abstractions (foundation)
    └── Hive.Functions
            ├── Depends on: Microsoft.Azure.Functions.Worker
            ├── Depends on: Microsoft.Azure.Functions.Worker.Extensions.Http
            └── Compatible with: Hive.OpenTelemetry, Hive.MicroServices.CORS
```

---

## 2. Core Architecture

### 2.1 Azure Functions Execution Model vs. Hive's IHost Model

**Current Hive Model (Api/Job/etc.):**
```
Application Start
  ↓
Build IHost → IServiceProvider
  ↓
Configure Middleware Pipeline
  ↓
Start Application (blocks)
  ↓
Handle Requests (long-lived)
  ↓
Graceful Shutdown
```

**Azure Functions Model:**
```
Function App Start (once per cold start)
  ↓
Build IServiceProvider (via IFunctionsWorkerApplicationBuilder)
  ↓
Function Invocation (ephemeral, per-trigger)
  ↓
  ├─ HTTP Trigger → FunctionContext + HttpRequestData
  ├─ Timer Trigger → FunctionContext + TimerInfo
  ├─ Queue Trigger → FunctionContext + QueueMessage
  └─ (etc.)
  ↓
Function Execution (scoped DI)
  ↓
Return Response (if applicable)
```

**Key Differences:**
- **No ASP.NET Core Pipeline** - Functions use `FunctionContext`, not `HttpContext`
- **Trigger-based Activation** - Not request/response only
- **Ephemeral Execution** - Each invocation is isolated
- **Azure-managed Lifecycle** - No direct control over host startup/shutdown

### 2.2 Design Decision: Use Isolated Worker Model

**Azure Functions supports two hosting models:**

1. **In-Process** (legacy) - Functions run in same process as host, tightly coupled to .NET version
2. **Isolated Worker** (recommended) - Functions run in separate worker process, version-independent

**Choice: Use Isolated Worker Model** because:
- Better isolation and stability
- More control over middleware pipeline
- Aligns with .NET 10 / C# 14 requirements
- Microsoft's recommended approach going forward

---

## 3. Proposed API Design

### 3.1 Function Definition Pattern

**Option A: Hive-Style Function Host (Recommended)**

```csharp
// demo/Hive.Functions.Demo/Program.cs
using Hive.Functions;
using Hive.OpenTelemetry;
using Microsoft.Azure.Functions.Worker;

var functionHost = new FunctionHost("hive-functions-demo")
    .WithOpenTelemetry(
        logging: builder => { /* custom logging config */ },
        tracing: builder => { /* custom tracing config */ }
    )
    .ConfigureServices((services, config) =>
    {
        services.AddSingleton<IWeatherService, WeatherService>();
        services.AddValidatorsFromAssemblyContaining<Program>();
    })
    .ConfigureFunctions(builder =>
    {
        // Azure Functions specific middleware
        builder.ConfigureFunctionsWebApplication(app =>
        {
            // Add function-level middleware (runs per-invocation)
            app.UseFunctionExecutionMiddleware(); // OpenTelemetry tracing
            app.UseAuthenticationMiddleware();    // Auth validation
        });
    });

await functionHost.RunAsync();
```

**Function Implementation:**

```csharp
// demo/Hive.Functions.Demo/Functions/WeatherFunction.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Hive.Functions;

public class WeatherFunction
{
    private readonly IWeatherService weatherService;
    private readonly ILogger<WeatherFunction> logger;

    // Standard DI - services from FunctionHost
    public WeatherFunction(
        IWeatherService weatherService,
        ILogger<WeatherFunction> logger)
    {
        this.weatherService = weatherService;
        this.logger = logger;
    }

    [Function("GetWeather")]
    public async Task<HttpResponseData> GetWeather(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "weather/{city}")]
        HttpRequestData req,
        string city,
        FunctionContext context)
    {
        logger.LogInformation("Processing weather request for {City}", city);

        var forecast = await weatherService.GetForecastAsync(city);

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(forecast);
        return response;
    }

    [Function("WeatherTimer")]
    public async Task WeatherTimer(
        [TimerTrigger("0 */5 * * * *")] TimerInfo timer,
        FunctionContext context)
    {
        logger.LogInformation("Timer trigger executed at {Time}", DateTime.UtcNow);
        await weatherService.RefreshCacheAsync();
    }
}
```

### 3.2 FunctionHost Implementation

```csharp
// hive.functions/src/Hive.Functions/FunctionHost.cs
namespace Hive.Functions;

public class FunctionHost : IFunctionHost
{
    public string Name { get; }
    public string Id { get; }
    public IConfigurationRoot ConfigurationRoot { get; private set; }
    public List<MicroServiceExtension> Extensions { get; } = new();

    private readonly List<Action<IServiceCollection, IConfiguration>> configureActions = new();
    private readonly List<Action<IFunctionsWorkerApplicationBuilder>> functionConfigureActions = new();

    public FunctionHost(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Id = Guid.NewGuid().ToString();

        // Core configuration action
        configureActions.Add((services, config) =>
        {
            services.AddSingleton<IFunctionHost>(this);
            services.AddApplicationInsightsTelemetryWorkerService();
        });
    }

    public FunctionHost ConfigureServices(
        Action<IServiceCollection, IConfiguration> configure)
    {
        configureActions.Add(configure ?? throw new ArgumentNullException(nameof(configure)));
        return this;
    }

    public FunctionHost ConfigureFunctions(
        Action<IFunctionsWorkerApplicationBuilder> configure)
    {
        functionConfigureActions.Add(configure ?? throw new ArgumentNullException(nameof(configure)));
        return this;
    }

    public FunctionHost RegisterExtension<TExtension>()
        where TExtension : MicroServiceExtension
    {
        var extension = (TExtension)Activator.CreateInstance(typeof(TExtension), this)!;
        Extensions.Add(extension);

        // Extension participates in service configuration
        configureActions.Add((services, config) =>
        {
            extension.ConfigureServices(services, config);
        });

        return this;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var host = CreateHostBuilder();
        await host.RunAsync(cancellationToken);
    }

    private IHost CreateHostBuilder()
    {
        var builder = new HostBuilder();

        // Load configuration (same as MicroService)
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
            config.AddJsonFile("appsettings.shared.json", optional: true);
            config.AddEnvironmentVariables();
        });

        // Configure Azure Functions Worker
        builder.ConfigureFunctionsWebApplication((IFunctionsWorkerApplicationBuilder app) =>
        {
            // Apply all function-specific configuration
            foreach (var action in functionConfigureActions)
            {
                action(app);
            }
        });

        // Configure services (DI container)
        builder.ConfigureServices((context, services) =>
        {
            ConfigurationRoot = (IConfigurationRoot)context.Configuration;

            // Apply all service configuration actions
            foreach (var action in configureActions)
            {
                action(services, context.Configuration);
            }
        });

        return builder.Build();
    }
}
```

---

## 4. Reusable Components

### 4.1 What Can Be Reused from Existing Hive

✅ **Directly Reusable:**
1. **DI Container Setup** - `IServiceCollection` configuration pattern
2. **Configuration System** - `appsettings.json`, environment variables, validation
3. **Extension Registration** - `RegisterExtension<T>()` pattern
4. **OpenTelemetry Integration** - Logging, tracing, metrics via `Hive.OpenTelemetry`
5. **Validation Patterns** - `ConfigureValidatedOptions<T>()`, FluentValidation
6. **Testing Utilities** - `Hive.Testing` traits, `InTestClass<T>()`

⚠️ **Requires Adaptation:**
1. **CORS Extension** - Need Functions-specific middleware (see below)
2. **Health Checks** - Functions don't expose `/health` endpoints natively (use Durable Functions monitoring or App Insights)
3. **Middleware Pipeline** - Functions middleware is per-invocation, not per-application

❌ **Not Applicable:**
1. **Probe Endpoints** (`/startup`, `/readiness`, `/liveness`) - Azure handles this
2. **Graceful Shutdown** - Managed by Azure Functions runtime
3. **ASP.NET Core Middleware** - `IApplicationBuilder` not available
4. **Pipeline Modes** - Functions don't use `MicroServicePipelineMode`

### 4.2 Extension Adaptation Examples

#### Example 1: OpenTelemetry Extension (Fully Reusable)

```csharp
// Already works! Just register it:
var functionHost = new FunctionHost("demo")
    .WithOpenTelemetry(
        logging: builder => builder.AddConsole(),
        tracing: builder => builder.AddHttpClientInstrumentation()
    );

// Extension.cs in Hive.OpenTelemetry already supports this via:
// - ILoggingBuilder configuration
// - Resource attributes (service.name, service.instance.id)
// - OTLP exporter configuration
```

**Why it works:**
- OpenTelemetry extension only touches `IServiceCollection` and `ILoggingBuilder`
- Doesn't depend on ASP.NET Core middleware
- Functions runtime provides `ILoggingBuilder` in `ConfigureServices`

#### Example 2: CORS Extension (Needs Adaptation)

**Current Implementation (ASP.NET Core):**
```csharp
// hive.microservices/src/Hive.MicroServices/CORS/Extension.cs
public class Extension : MicroServiceExtension
{
    public override IServiceCollection ConfigureServices(
        IServiceCollection services, IConfiguration configuration)
    {
        services.PreConfigureValidatedOptions<CorsOptions>(configuration.GetSection("CORS"));
        services.AddCors(options => { /* policy setup */ });
        return services;
    }
}

// Applied in pipeline via app.UseCors()
```

**Functions Adaptation:**
```csharp
// hive.functions/src/Hive.Functions/Extensions/CorsExtension.cs
public class FunctionCorsExtension : MicroServiceExtension
{
    public override IServiceCollection ConfigureServices(
        IServiceCollection services, IConfiguration configuration)
    {
        // Same configuration, but applied via middleware
        services.PreConfigureValidatedOptions<CorsOptions>(configuration.GetSection("CORS"));
        return services;
    }

    // NEW: Function-specific middleware registration
    public void ConfigureFunctionsMiddleware(IFunctionsWorkerApplicationBuilder builder)
    {
        builder.Use(async (FunctionContext context, Func<Task> next) =>
        {
            if (context.IsHttpRequest(out var httpReqData))
            {
                var corsOptions = context.InstanceServices
                    .GetRequiredService<IOptions<CorsOptions>>().Value;

                // Apply CORS headers based on options
                ApplyCorsHeaders(httpReqData, corsOptions);
            }

            await next();
        });
    }
}
```

**Registration:**
```csharp
var functionHost = new FunctionHost("demo")
    .WithCors(config.GetSection("CORS")) // Extension method
    .ConfigureFunctions(builder =>
    {
        // CORS middleware applied automatically by extension
    });
```

---

## 5. Implementation Roadmap

### Phase 1: Foundation (MVP)

**Goal:** Basic Function hosting with DI and configuration support

**Deliverables:**
1. Create `hive.functions` module following repository structure
2. Implement `FunctionHost` class
3. Implement `IFunctionHost` interface (minimal subset of `IMicroService`)
4. Support for `RegisterExtension<T>()` pattern
5. Configuration loading (appsettings.json, environment variables)
6. Basic demo function (HTTP trigger)

**Files to Create:**
- `hive.functions/src/Hive.Functions/Hive.Functions.csproj`
- `hive.functions/src/Hive.Functions/IFunctionHost.cs`
- `hive.functions/src/Hive.Functions/FunctionHost.cs`
- `hive.functions/src/Hive.Functions/Extensions/IMicroServiceExtensions.cs`
- `hive.functions/demo/Hive.Functions.Demo/Hive.Functions.Demo.csproj`
- `hive.functions/demo/Hive.Functions.Demo/Program.cs`
- `hive.functions/demo/Hive.Functions.Demo/Functions/HttpTriggerFunction.cs`
- `hive.functions/demo/Hive.Functions.Demo/host.json`

**NuGet Packages Required:**
```xml
<PackageReference Include="Microsoft.Azure.Functions.Worker" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" />
<ProjectReference Include="../../../hive.core/src/Hive.Abstractions/Hive.Abstractions.csproj" />
```

### Phase 2: Extension Integration

**Goal:** Enable existing Hive extensions to work with Functions

**Deliverables:**
1. OpenTelemetry integration test
2. Configuration validation integration
3. Logging integration (structured logging)
4. Extension lifecycle documentation

**Test Cases:**
```csharp
[Fact]
[IntegrationTest]
public async Task FunctionHost_WithOpenTelemetry_ShouldExportTraces()
{
    var functionHost = new FunctionHost("test")
        .WithOpenTelemetry()
        .InTestClass<FunctionHostTests>();

    // Verify telemetry configuration applied
    // Verify traces exported to OTLP endpoint
}
```

### Phase 3: Advanced Features

**Goal:** Function-specific extensions and middleware

**Deliverables:**
1. CORS middleware for Functions
2. Authentication/Authorization middleware
3. Custom response adapters (structured error responses)
4. Durable Functions support investigation

**Extension Examples:**
```csharp
functionHost
    .WithCors(corsOptions)
    .WithAuthentication(authOptions)
    .WithStructuredErrorHandling();
```

### Phase 4: Production Readiness

**Goal:** Testing, documentation, deployment examples

**Deliverables:**
1. Comprehensive integration tests
2. Deployment documentation (Bicep/Terraform templates)
3. Performance benchmarks vs. standard Functions
4. Migration guide from standalone Functions

---

## 6. Key Design Considerations

### 6.1 IFunctionHost vs IMicroService

**Option A: IFunctionHost extends IMicroService (Inheritance)**

```csharp
public interface IFunctionHost : IMicroService
{
    // Inherits: Name, Id, ConfigurationRoot, Extensions, RegisterExtension<T>()
    // Functions-specific additions:
    IFunctionsWorkerApplicationBuilder FunctionsBuilder { get; }
}
```

**Pros:**
- Existing extensions work without modification
- Code reuse maximized
- Consistent API surface

**Cons:**
- Functions don't have `PipelineMode`, `IsReady`, `IsStarted` (not applicable)
- Functions don't expose `IHost` directly (managed by Azure runtime)
- Conceptual mismatch (Functions aren't long-lived services)

**Option B: IFunctionHost as separate interface (Composition - RECOMMENDED)**

```csharp
public interface IFunctionHost
{
    string Name { get; }
    string Id { get; }
    IConfigurationRoot ConfigurationRoot { get; }
    List<MicroServiceExtension> Extensions { get; }

    IFunctionHost ConfigureServices(Action<IServiceCollection, IConfiguration> configure);
    IFunctionHost ConfigureFunctions(Action<IFunctionsWorkerApplicationBuilder> configure);
    IFunctionHost RegisterExtension<TExtension>() where TExtension : MicroServiceExtension;

    Task RunAsync(CancellationToken cancellationToken = default);
}
```

**Pros:**
- Clean separation of concerns
- No conceptual pollution (no unused `IsReady`, `PipelineMode`, etc.)
- Extensions can explicitly declare "I support Functions" if they override new virtual methods

**Cons:**
- Some code duplication with `MicroService`
- Extensions might need adaptation (but most won't)

**Recommendation: Option B (Composition)** - Cleaner boundaries, more honest API

### 6.2 Extension Compatibility Strategy

**Three categories of extensions:**

1. **Fully Compatible** (no changes needed)
   - `Hive.OpenTelemetry` - Only touches `IServiceCollection` and `ILoggingBuilder`
   - Validation extensions - Only touch configuration
   - Logging extensions - Already use `ILoggingBuilder`

2. **Adaptation Required** (new method or middleware)
   - CORS - Needs Functions middleware implementation
   - Authentication - Needs Functions middleware implementation
   - Health checks - Not applicable (use Application Insights)

3. **Not Applicable**
   - Probe endpoints - Managed by Azure
   - Graceful shutdown - Managed by Azure
   - ASP.NET Core routing - Not available in Functions

**Strategy:**
```csharp
public abstract class MicroServiceExtension
{
    // Existing methods (work for both MicroService and FunctionHost)
    public virtual IServiceCollection ConfigureServices(
        IServiceCollection services, IConfiguration configuration)
        => services;

    // NEW: Optional Functions-specific configuration
    public virtual void ConfigureFunctions(
        IFunctionsWorkerApplicationBuilder builder)
    {
        // Default: no-op
        // Extensions that need Functions middleware override this
    }
}
```

### 6.3 Testing Strategy

**Reuse existing testing infrastructure:**

```csharp
using Hive.Testing;

[Fact]
[IntegrationTest]
public async Task FunctionHost_WithOpenTelemetry_ShouldStart()
{
    var functionHost = new FunctionHost("test-function")
        .WithOpenTelemetry()
        .InTestClass<FunctionHostTests>() // Reuse existing test extension
        .ConfigureServices((services, _) =>
        {
            services.AddSingleton<ITestService, TestService>();
        });

    // Verify services registered
    var serviceProvider = functionHost.GetServiceProvider(); // Helper for tests
    var testService = serviceProvider.GetRequiredService<ITestService>();
    testService.Should().NotBeNull();
}
```

**Integration with Azure Functions Core Tools:**
```bash
# Run demo locally
cd hive.functions/demo/Hive.Functions.Demo
func start

# Expected output:
# [2026-01-12T10:00:00.000Z] Hive FunctionHost 'hive-functions-demo' starting...
# [2026-01-12T10:00:00.100Z] OpenTelemetry configured (endpoint: http://localhost:4317)
# [2026-01-12T10:00:00.200Z] Function 'GetWeather' registered
# [2026-01-12T10:00:00.300Z] Function 'WeatherTimer' registered
# [2026-01-12T10:00:00.400Z] Host started
```

---

## 7. Example Scenarios

### Scenario 1: HTTP Trigger with OpenTelemetry

```csharp
// Program.cs
var functionHost = new FunctionHost("api-functions")
    .WithOpenTelemetry(
        logging: builder => builder.SetMinimumLevel(LogLevel.Information),
        tracing: builder => builder.AddHttpClientInstrumentation()
    )
    .ConfigureServices((services, config) =>
    {
        services.AddHttpClient<IExternalApiClient, ExternalApiClient>();
        services.AddSingleton<IValidator<CreateOrderRequest>, CreateOrderValidator>();
    });

await functionHost.RunAsync();
```

```csharp
// Functions/OrderFunction.cs
public class OrderFunction
{
    private readonly IExternalApiClient apiClient;
    private readonly IValidator<CreateOrderRequest> validator;
    private readonly ILogger<OrderFunction> logger;

    public OrderFunction(
        IExternalApiClient apiClient,
        IValidator<CreateOrderRequest> validator,
        ILogger<OrderFunction> logger)
    {
        this.apiClient = apiClient;
        this.validator = validator;
        this.logger = logger;
    }

    [Function("CreateOrder")]
    public async Task<HttpResponseData> CreateOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")]
        HttpRequestData req,
        FunctionContext context)
    {
        var request = await req.ReadFromJsonAsync<CreateOrderRequest>();

        // Validation (FluentValidation from Hive)
        var validationResult = await validator.ValidateAsync(request!);
        if (!validationResult.IsValid)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(new { errors = validationResult.Errors });
            return response;
        }

        // OpenTelemetry automatically traces this HTTP call
        var result = await apiClient.CreateOrderAsync(request);

        logger.LogInformation("Order {OrderId} created successfully", result.OrderId);

        var successResponse = req.CreateResponse(HttpStatusCode.Created);
        await successResponse.WriteAsJsonAsync(result);
        return successResponse;
    }
}
```

### Scenario 2: Timer Trigger with Background Processing

```csharp
// Program.cs
var functionHost = new FunctionHost("batch-processor")
    .WithOpenTelemetry()
    .ConfigureServices((services, config) =>
    {
        services.AddSingleton<IBatchProcessor, BatchProcessor>();
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("Database")));
    });

await functionHost.RunAsync();
```

```csharp
// Functions/BatchProcessorFunction.cs
public class BatchProcessorFunction
{
    private readonly IBatchProcessor processor;
    private readonly ILogger<BatchProcessorFunction> logger;

    public BatchProcessorFunction(
        IBatchProcessor processor,
        ILogger<BatchProcessorFunction> logger)
    {
        this.processor = processor;
        this.logger = logger;
    }

    [Function("ProcessBatch")]
    public async Task ProcessBatch(
        [TimerTrigger("0 0 */4 * * *")] TimerInfo timer, // Every 4 hours
        FunctionContext context)
    {
        logger.LogInformation("Batch processing started at {Time}", DateTime.UtcNow);

        try
        {
            var result = await processor.ProcessPendingItemsAsync();
            logger.LogInformation("Processed {Count} items successfully", result.ProcessedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Batch processing failed");
            throw; // Azure Functions runtime handles retry logic
        }
    }
}
```

### Scenario 3: Queue Trigger with Durable Functions (Future Enhancement)

```csharp
// Potential future API for Durable Functions orchestration
var functionHost = new FunctionHost("orchestrator")
    .WithOpenTelemetry()
    .WithDurableFunctions() // Future extension
    .ConfigureServices((services, config) =>
    {
        services.AddSingleton<IOrderService, OrderService>();
        services.AddSingleton<IPaymentService, PaymentService>();
    });
```

---

## 8. Migration Path

### Migrating Existing Azure Functions to Hive.Functions

**Before (Standard Azure Functions):**
```csharp
// Program.cs
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<IWeatherService, WeatherService>();
    })
    .Build();

await host.RunAsync();
```

**After (Hive.Functions):**
```csharp
// Program.cs
var functionHost = new FunctionHost("weather-functions")
    .WithOpenTelemetry() // Replaces Application Insights boilerplate
    .ConfigureServices((services, config) =>
    {
        services.AddSingleton<IWeatherService, WeatherService>();
    });

await functionHost.RunAsync();
```

**Benefits:**
- Reduced boilerplate (OpenTelemetry setup, Application Insights, etc.)
- Consistent DI and configuration patterns with other Hive services
- Extension ecosystem (CORS, validation, logging, etc.)
- Testing utilities from `Hive.Testing`

---

## 9. Open Questions & Future Enhancements

### Open Questions

1. **Should `IFunctionHost` implement `IMicroService` or remain separate?**
   - **Recommendation**: Keep separate for cleaner API surface

2. **How to handle Durable Functions orchestration?**
   - Durable Functions have their own DI container scope
   - Might need `FunctionHost.WithDurableFunctions()` extension
   - Investigation needed in Phase 3

3. **What about non-HTTP triggers (Blob, Queue, EventHub)?**
   - MVP focuses on HTTP and Timer triggers
   - Other triggers should work via standard Azure Functions attributes
   - No Hive-specific integration needed (just DI)

4. **How to test functions locally?**
   - Use Azure Functions Core Tools (`func start`)
   - Integration tests can use `FunctionHost.GetServiceProvider()` for DI verification
   - End-to-end tests require Azure Functions emulator or actual Azure deployment

### Future Enhancements

1. **Function Middleware Builder**
   ```csharp
   functionHost.ConfigureFunctions(builder =>
   {
       builder.UseAuthentication()
              .UseAuthorization()
              .UseCorrelationId()
              .UseStructuredErrorHandling();
   });
   ```

2. **Declarative Function Registration**
   ```csharp
   functionHost.RegisterFunction<WeatherFunction>()
               .RegisterFunction<OrderFunction>()
               .WithAutoDiscovery(); // Scans assembly for [Function] attributes
   ```

3. **Binding Helpers**
   ```csharp
   [Function("CreateOrder")]
   public async Task<IActionResult> CreateOrder(
       [HttpTrigger] HttpRequestData req,
       [Validated] CreateOrderRequest request, // Auto-validates using FluentValidation
       FunctionContext context)
   {
       // Request already validated, just process
   }
   ```

4. **Hive.Functions.Testing**
   ```csharp
   var testHost = FunctionHostTestBuilder
       .Create("test-function")
       .WithInMemoryServices()
       .WithMockHttpClient()
       .Build();

   var result = await testHost.InvokeAsync<WeatherFunction>(
       nameof(WeatherFunction.GetWeather),
       new { city = "Seattle" }
   );

   result.StatusCode.Should().Be(HttpStatusCode.OK);
   ```

---

## 10. Success Criteria

**MVP is successful when:**

1. ✅ A Hive.Functions demo can be created following the same pattern as Hive.MicroServices demos
2. ✅ `FunctionHost` supports DI registration via `ConfigureServices()`
3. ✅ `FunctionHost` supports extension registration via `RegisterExtension<T>()`
4. ✅ OpenTelemetry extension works without modification
5. ✅ Configuration loading (appsettings.json, environment variables) works
6. ✅ Function can be run locally via `func start`
7. ✅ Function can be deployed to Azure Functions
8. ✅ Repository structure follows `hive.functions/src/`, `hive.functions/tests/`, `hive.functions/demo/` layout

**Phase 2 is successful when:**

1. ✅ At least 3 existing Hive extensions work with Functions (OpenTelemetry, Validation, Logging)
2. ✅ Integration tests verify extension behavior
3. ✅ Documentation explains extension compatibility

**Production-ready when:**

1. ✅ CORS and Authentication extensions adapted for Functions
2. ✅ Comprehensive test coverage (unit + integration)
3. ✅ Deployment examples (Bicep/Terraform)
4. ✅ Migration guide from standard Functions
5. ✅ Performance benchmarks published

---

## 11. CORS Module Extraction Analysis

### 11.1 Current State Assessment

**Current Location:** `hive.microservices/src/Hive.MicroServices/CORS/`

**Files:**
- `Extension.cs` - Main CORS extension (108 lines)
- `Options.cs` - Configuration model (24 lines)
- `OptionsValidator.cs` - FluentValidation validator (48 lines)
- `CORSPolicy.cs` - Individual policy model
- `CORSPolicyValidator.cs` - FluentValidation validator for policies
- `README.md` - Comprehensive documentation

**Coupling Analysis:**

| Aspect | Portability | Issue |
|--------|------------|-------|
| Configuration models (Options, CORSPolicy) | ✅ 100% Portable | Pure POCOs, no dependencies |
| Validators (OptionsValidator, CORSPolicyValidator) | ✅ 95% Portable | Only uses `IMicroService.Environment` (already in abstraction) |
| Service registration logic | ⚠️ 70% Portable | Casts to concrete `MicroService` type for logging (lines 62, 78, 92) |
| ASP.NET Core middleware application | ❌ Not Portable | Uses `app.UseCors()` - ASP.NET Core specific |

**Critical Coupling Point:**

```csharp
// Extension.cs:62, 78, 85
((MicroService)Service).Logger.LogInformationPolicyConfigured(AllowAnyPolicyName);
```

This cast to the concrete `MicroService` type creates tight coupling that prevents extraction.

### 11.2 Extraction Strategy - Option A: Shared Hive.CORS Module

**Goal:** Create a single, reusable CORS module that works across all Hive hosting models (MicroServices, Functions, future models).

**Module Structure:**
```
hive.cors/
  ├── src/
  │   └── Hive.CORS/
  │       ├── Hive.CORS.csproj
  │       ├── Configuration/
  │       │   ├── Options.cs                    # Pure configuration models
  │       │   ├── OptionsValidator.cs           # FluentValidation rules
  │       │   ├── CORSPolicy.cs                 # Policy model
  │       │   └── CORSPolicyValidator.cs        # Policy validation
  │       ├── Abstractions/
  │       │   └── ICORSMiddleware.cs            # Hosting-agnostic interface
  │       ├── Extensions/
  │       │   └── Extension.cs                  # Base CORS extension
  │       └── README.md
  │
  ├── tests/
  │   └── Hive.CORS.Tests/
  │       ├── Hive.CORS.Tests.csproj
  │       ├── OptionsValidatorTests.cs
  │       └── CORSPolicyValidatorTests.cs
  │
  └── adapters/                                 # Optional: Hosting-specific adapters
      ├── Hive.CORS.AspNetCore/                # For Hive.MicroServices
      │   ├── AspNetCoreExtension.cs
      │   └── Hive.CORS.AspNetCore.csproj
      └── Hive.CORS.Functions/                 # For Hive.Functions
          ├── FunctionsExtension.cs
          └── Hive.CORS.Functions.csproj
```

**Dependencies:**
```
Hive.Abstractions
    ├── Hive.CORS (core - portable configuration/validation)
    │   ├── Hive.CORS.AspNetCore (ASP.NET Core middleware adapter)
    │   └── Hive.CORS.Functions (Azure Functions middleware adapter)
    │
    ├── Hive.MicroServices → depends on Hive.CORS.AspNetCore
    └── Hive.Functions → depends on Hive.CORS.Functions
```

### 11.3 Extraction Strategy - Option B: Keep CORS Embedded (Simpler)

**Rationale:** CORS is fundamentally HTTP-specific and tightly coupled to hosting middleware.

**Alternative Approach:**
- Keep CORS in `Hive.MicroServices.CORS` for ASP.NET Core services
- Create separate `Hive.Functions.CORS` for Azure Functions
- Share only the configuration models via a lightweight `Hive.CORS.Abstractions` package

**Module Structure:**
```
hive.cors.abstractions/                        # Shared configuration only
  └── src/
      └── Hive.CORS.Abstractions/
          ├── Options.cs                        # Pure POCO
          ├── CORSPolicy.cs                     # Pure POCO
          └── Hive.CORS.Abstractions.csproj

hive.microservices/src/Hive.MicroServices/     # Keep existing
  └── CORS/
      ├── Extension.cs                          # ASP.NET Core specific
      ├── OptionsValidator.cs                   # References Abstractions
      └── (uses Hive.CORS.Abstractions models)

hive.functions/src/Hive.Functions/             # New implementation
  └── CORS/
      ├── Extension.cs                          # Functions specific
      ├── OptionsValidator.cs                   # References Abstractions
      └── (uses Hive.CORS.Abstractions models)
```

**Dependencies:**
```
Hive.CORS.Abstractions (lightweight - just POCOs)
    ├── Hive.MicroServices.CORS → depends on it
    └── Hive.Functions.CORS → depends on it
```

### 11.4 Required Changes for Extraction (Option A)

**Step 1: Fix Logger Coupling**

**Before:**
```csharp
// Extension.cs:62
((MicroService)Service).Logger.LogInformationPolicyConfigured(AllowAnyPolicyName);
```

**After (Option 1 - Add Logger to IMicroService):**
```csharp
// Hive.Abstractions/IMicroService.cs
public interface IMicroService
{
    // ... existing properties ...
    ILogger Logger { get; } // NEW - expose logger
}

// Extension.cs:62
Service.Logger.LogInformationPolicyConfigured(AllowAnyPolicyName);
```

**After (Option 2 - Inject via DI - RECOMMENDED):**
```csharp
// Extension.cs
public class Extension : MicroServiceExtension
{
    private readonly ILogger<Extension> _logger;

    public Extension(IMicroService service, ILogger<Extension> logger) : base(service)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override IServiceCollection ConfigureServices(...)
    {
        // Use _logger instead of ((MicroService)Service).Logger
        _logger.LogInformationPolicyConfigured(AllowAnyPolicyName);
    }
}
```

**Recommendation:** Use Option 2 (DI injection) because:
- ✅ Doesn't pollute `IMicroService` interface with implementation details
- ✅ Follows standard .NET logging patterns
- ✅ Makes testing easier (can mock ILogger)
- ✅ Allows different logger categories per extension

**Step 2: Abstract Middleware Application**

Create hosting-agnostic interface:

```csharp
// Hive.CORS/Abstractions/ICORSMiddleware.cs
namespace Hive.CORS.Abstractions;

public interface ICORSMiddleware
{
    /// <summary>
    /// Applies CORS policy to the hosting pipeline
    /// </summary>
    void ApplyPolicy(string? policyName);
}
```

**Step 3: Implement ASP.NET Core Adapter**

```csharp
// Hive.CORS.AspNetCore/AspNetCoreMiddleware.cs
namespace Hive.CORS.AspNetCore;

public class AspNetCoreMiddleware : ICORSMiddleware
{
    private readonly IApplicationBuilder _app;

    public AspNetCoreMiddleware(IApplicationBuilder app)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));
    }

    public void ApplyPolicy(string? policyName)
    {
        _app.UseCors(policyName ?? "default");
    }
}
```

**Step 4: Implement Functions Adapter**

```csharp
// Hive.CORS.Functions/FunctionsMiddleware.cs
namespace Hive.CORS.Functions;

public class FunctionsMiddleware : ICORSMiddleware
{
    private readonly IFunctionsWorkerApplicationBuilder _builder;
    private readonly Options _options;

    public FunctionsMiddleware(
        IFunctionsWorkerApplicationBuilder builder,
        IOptions<Options> options)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public void ApplyPolicy(string? policyName)
    {
        _builder.Use(async (FunctionContext context, Func<Task> next) =>
        {
            if (context.IsHttpRequest(out var httpReqData))
            {
                ApplyCorsHeaders(httpReqData, context.InstanceServices);
            }
            await next();
        });
    }

    private void ApplyCorsHeaders(HttpRequestData request, IServiceProvider services)
    {
        var policy = _options.Policies.FirstOrDefault(p => p.Name == policyName);
        if (policy == null) return;

        var origin = request.Headers.GetValues("Origin").FirstOrDefault();
        if (origin != null && policy.AllowedOrigins.Contains(origin))
        {
            request.FunctionContext.GetHttpResponseData()
                ?.Headers.Add("Access-Control-Allow-Origin", origin);
        }

        // Add other CORS headers based on policy configuration
    }
}
```

### 11.5 Migration Impact Analysis

**Files Modified:**
1. ✅ Create new `hive.cors/` module structure
2. ✅ Move CORS files from `Hive.MicroServices/CORS/` to `Hive.CORS/Configuration/`
3. ⚠️ Update `Extension.cs` to remove `MicroService` cast, use DI logger
4. ⚠️ Update all pipeline modes (Api, GraphQL, Grpc, Job) to reference new module
5. ⚠️ Update `Hive.MicroServices.csproj` to reference `Hive.CORS.AspNetCore`
6. ⚠️ Move tests from `Hive.MicroServices.Tests` to `Hive.CORS.Tests`

**Build Impact:**
- All projects that reference `Hive.MicroServices` need to also reference `Hive.CORS` (or adapter)
- Solution file needs new project entries for `hive.cors` module
- `Directory.Packages.props` unchanged (no new NuGet dependencies)

**Runtime Impact:**
- ✅ Zero runtime impact - same behavior, different assembly
- ✅ Configuration format unchanged (still `Hive:CORS` section)
- ✅ Existing tests continue to work with minor adjustments

### 11.6 Recommendation

**RECOMMENDED: Option A Modified - Move CORS Abstractions to Hive.Abstractions**

After further analysis, the cleanest approach is to **move shared CORS configuration and validation to `Hive.Abstractions`** rather than creating a new module or keeping everything duplicated.

**Why Hive.Abstractions is the Right Place:**

1. **Precedent Exists** - `Hive.Abstractions` already contains:
   - Configuration utilities (`PreConfigureValidatedOptions`, `ConfigureValidatedOptions`)
   - Validation base classes (`FluentOptionsValidator`, `MiniOptionsValidator`)
   - Extension base class (`MicroServiceExtension`)
   - Shared constants (`Constants.Environment`, `Constants.Headers`)

2. **Dependency Graph** - Already a required dependency:
   ```
   Hive.Abstractions
       ├── Hive.MicroServices
       └── Hive.Functions
   ```

3. **Package Semantics** - Configuration models and validators ARE abstractions

4. **Zero Breaking Changes** - Just internal refactoring within the package

**Implementation Plan:**

**Step 1: Clean up ASP.NET Core dependency**
- Remove `ToCORSPolicyBuilderAction()` method from `CORSPolicy.cs` (uses `Microsoft.AspNetCore.Cors.Infrastructure`)
- This method should be in the ASP.NET Core specific extension, not the abstraction

**Step 2: Move to Hive.Abstractions**
```bash
# Move pure configuration models and validators
git mv hive.microservices/src/Hive.MicroServices/CORS/Options.cs \
       hive.core/src/Hive.Abstractions/CORS/Options.cs

git mv hive.microservices/src/Hive.MicroServices/CORS/CORSPolicy.cs \
       hive.core/src/Hive.Abstractions/CORS/CORSPolicy.cs

git mv hive.microservices/src/Hive.MicroServices/CORS/OptionsValidator.cs \
       hive.core/src/Hive.Abstractions/CORS/OptionsValidator.cs

git mv hive.microservices/src/Hive.MicroServices/CORS/CORSPolicyValidator.cs \
       hive.core/src/Hive.Abstractions/CORS/CORSPolicyValidator.cs

# Keep Extension.cs in Hive.MicroServices (ASP.NET Core specific)
```

**Step 3: Update namespaces**
- Change moved files from `namespace Hive.MicroServices.CORS;` to `namespace Hive.CORS;`

**Step 4: Update Extensions to use new namespace**
```csharp
// hive.microservices/src/Hive.MicroServices/CORS/Extension.cs
using Hive.CORS; // ← Import from Hive.Abstractions

// hive.functions/src/Hive.Functions/CORS/Extension.cs
using Hive.CORS; // ← Same namespace, same configuration!
```

**Benefits:**
- ✅ **Shared Configuration** - Both hosting models use identical configuration
- ✅ **Shared Validation** - Same validation rules everywhere
- ✅ **No New Dependencies** - `Hive.Abstractions` already referenced
- ✅ **Follows Pattern** - Matches existing structure in Abstractions
- ✅ **Clean Separation** - Abstractions are pure, middleware is hosting-specific
- ✅ **Zero Breaking Changes** - Just internal reorganization

**Why NOT create separate Hive.CORS module:**
1. ⏱️ **Unnecessary Complexity** - Would create new module for just 4 files
2. 📦 **Dependency Management** - Would need to add new dependency to all projects
3. 🎯 **YAGNI** - `Hive.Abstractions` already serves this purpose
4. 🔄 **Pattern Violation** - Other shared config utilities are in Abstractions

### 11.7 Immediate Action Items for Functions Support

**For MVP (Hive.Functions):**

1. ✅ **Move** CORS abstractions to `Hive.Abstractions/CORS/`
   - Remove ASP.NET Core dependency from `CORSPolicy.cs`
   - Move `Options.cs`, `CORSPolicy.cs`, validators to `Hive.Abstractions`
   - Update namespaces to `Hive.CORS`

2. ✅ **Update** `Hive.MicroServices.CORS.Extension` to use `Hive.CORS` namespace

3. ✅ **Create** `Hive.Functions` CORS extension:
   ```csharp
   // hive.functions/src/Hive.Functions/CORS/Extension.cs
   using Hive.CORS; // ← Shared namespace from Hive.Abstractions
   using Microsoft.Azure.Functions.Worker;

   namespace Hive.Functions.CORS;

   public class Extension : MicroServiceExtension
   {
       public Extension(IFunctionHost functionHost) : base(functionHost) { }

       public override IServiceCollection ConfigureServices(
           IServiceCollection services,
           IConfiguration configuration)
       {
           // ✅ Same configuration loading as MicroServices!
           services.PreConfigureValidatedOptions<Options>(
               configuration.GetSection(Options.SectionKey)
           );
           return services;
       }

       public void ConfigureFunctions(IFunctionsWorkerApplicationBuilder builder)
       {
           builder.Use(async (FunctionContext context, Func<Task> next) =>
           {
               if (context.IsHttpRequest(out var httpReqData))
               {
                   var corsOptions = context.InstanceServices
                       .GetRequiredService<IOptions<Options>>().Value;

                   ApplyCorsHeaders(httpReqData, corsOptions);
               }
               await next();
           });
       }

       private void ApplyCorsHeaders(HttpRequestData request, Options options)
       {
           // Apply CORS headers based on policy configuration
           // Implementation similar to ASP.NET Core but using Functions APIs
       }
   }
   ```

4. ✅ **Document** configuration compatibility:
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
   Same configuration works for both MicroServices and Functions!

5. ✅ **Add tests** to verify CORS headers applied correctly in Functions context

**For Future (Post-MVP):**
- Move validation tests to `Hive.Abstractions.Tests`
- Consider if additional shared utilities should move to `Hive.Abstractions`
- Monitor for other cross-cutting concerns that could benefit from same pattern

---

## 12. Summary

**Hive.Functions leverages Hive's strengths:**
- ✅ DI container and configuration system
- ✅ Extension-based architecture
- ✅ OpenTelemetry integration
- ✅ Testing utilities
- ✅ Validation patterns

**Hive.Functions acknowledges differences:**
- ⚠️ No ASP.NET Core middleware pipeline (Functions have their own)
- ⚠️ No probe endpoints (Azure manages health)
- ⚠️ No graceful shutdown control (Azure manages lifecycle)
- ⚠️ Ephemeral execution model vs. long-running services

**Implementation approach:**
- 🎯 Create separate `IFunctionHost` interface (composition over inheritance)
- 🎯 Reuse `MicroServiceExtension` base class with optional `ConfigureFunctions()` override
- 🎯 Follow repository structure rules (`hive.functions/src/`, `/tests/`, `/demo/`)
- 🎯 Start with HTTP and Timer triggers (expand to other triggers later)
- 🎯 Prioritize OpenTelemetry integration in MVP

**Next steps:**
1. Get approval on architecture approach
2. Create `hive.functions` module structure
3. Implement `FunctionHost` MVP
4. Create demo function with OpenTelemetry
5. Iterate based on feedback

---

**End of Design Document**
