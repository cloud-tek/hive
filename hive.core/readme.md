# Hive.Core

Core abstractions and utilities for the Hive microservices framework.

## Overview

The `hive.core` module provides the foundational components for building microservices with Hive:

- **Hive.Abstractions** - Core abstractions, interfaces, and extension methods
- **Hive.Testing** - In-repo (unpublished, `IsPackable=false`) test helpers used by this repository's own test projects. The xUnit category attributes, `TestPortProvider` and `EnvironmentVariableScope` come from the `CloudTek.Testing` NuGet package

## Hive.Abstractions

### Configuration Extensions

Hive provides a powerful pre-configuration pattern that allows you to access and validate configuration **before** `IServiceProvider` is built. This is essential for configuration that affects service registration.

#### PreConfigureOptions

Load configuration into a strongly-typed options class before the service provider is built.

```csharp
var options = services.PreConfigureOptions<MyOptions>(
  configuration,
  () => "MySection");

// Options are immediately available
Console.WriteLine(options.Value.SomeSetting);
```

#### PreConfigureValidatedOptions

Load and validate configuration using **DataAnnotations** (MiniValidator):

```csharp
public class MyOptions
{
  [Required]
  [MinLength(3)]
  public string Name { get; set; } = string.Empty;
}

// Throws OptionsValidationException if validation fails
var options = services.PreConfigureValidatedOptions<MyOptions>(
  configuration,
  () => "MySection");
```

**With Custom Validation Delegate:**

```csharp
var options = services.PreConfigureValidatedOptions<MyOptions>(
  configuration,
  () => "MySection",
  opts => opts.Name?.StartsWith("Test") == true);
```

**With FluentValidation:**

```csharp
public class MyOptionsValidator : AbstractValidator<MyOptions>
{
  public MyOptionsValidator()
  {
    RuleFor(x => x.Name).NotEmpty().MinimumLength(3);
  }
}

var options = services.PreConfigureValidatedOptions<MyOptions, MyOptionsValidator>(
  configuration,
  () => "MySection");
```

#### PreConfigureOptionalValidatedOptions

Load and validate configuration from an **optional section**. Returns `null` if the section doesn't exist, allowing you to provide defaults.

**Use Case:** Configuration that should use sensible defaults when not provided.

```csharp
// Section doesn't exist - returns null
var options = services.PreConfigureOptionalValidatedOptions<MyOptions>(
  configuration,
  () => "OptionalSection");

// Use defaults when section is missing
var finalOptions = options?.Value ?? new MyOptions();
```

**With DataAnnotations:**

```csharp
var options = services.PreConfigureOptionalValidatedOptions<MyOptions>(
  configuration,
  () => "OptionalSection");
```

**With Custom Validation Delegate:**

```csharp
var options = services.PreConfigureOptionalValidatedOptions<MyOptions>(
  configuration,
  () => "OptionalSection",
  opts => opts.Name != null);
```

**With FluentValidation:**

```csharp
var options = services.PreConfigureOptionalValidatedOptions<MyOptions, MyOptionsValidator>(
  configuration,
  () => "OptionalSection");
```

#### Configuration Methods Comparison

| Method | Section Required? | Returns | Validates? | Use When |
|--------|-------------------|---------|------------|----------|
| `PreConfigureOptions` | ✅ Yes | `IOptions<T>` | ❌ No | Required config, no validation needed |
| `PreConfigureValidatedOptions` | ✅ Yes | `IOptions<T>` | ✅ Yes | Required config with validation |
| `PreConfigureOptionalValidatedOptions` | ❌ No | `IOptions<T>?` | ✅ Yes (if exists) | Optional config with defaults |

### Validation Methods

Hive supports three validation approaches:

1. **DataAnnotations** (via MiniValidator)
   - Uses standard .NET attributes: `[Required]`, `[Range]`, `[MinLength]`, etc.
   - Automatic validation without extra code

2. **Custom Delegate**
   - Simple validation logic: `opts => opts.Value > 0`
   - Best for single-property checks

3. **FluentValidation**
   - Complex validation rules with detailed error messages
   - Composable validators with child validators
   - Best practice for production applications

### MicroService Extensions

Extensions inherit from the generic `MicroServiceExtension<TExtension>` base class, where `TExtension` is the extension type itself. The constructor receives an `IMicroServiceCore`.

```csharp
public class MyExtension : MicroServiceExtension<MyExtension>
{
  public MyExtension(IMicroServiceCore service) : base(service)
  {
  }

  public override IServiceCollection ConfigureServices(
    IServiceCollection services,
    IMicroServiceCore microservice)
  {
    // Service registration
    services.AddSingleton<IMyService, MyService>();
    return services;
  }

  public override IApplicationBuilder Configure(
    IApplicationBuilder app,
    IMicroServiceCore microservice)
  {
    // Middleware pipeline configuration
    return app;
  }
}
```

Notes:

- Register extensions (`RegisterExtension<T>()` or a `With*` method) **before** the `Configure*Pipeline` call; the pipeline call takes a snapshot of the registered extensions.
- `ConfigureEndpoints` and `ConfigureBeforeReadinessProbe` are not currently invoked by the framework. To add routes use `.MapEndpoints(...)`.
- There is no `ConfigureHealthChecks` hook.

## Testing helpers

Test helpers come from three places.

### CloudTek.Testing (NuGet)

Custom xUnit trait attributes for categorizing tests (exactly one per test), plus `[Feature("ARTIFACT/REQ")]` for requirement traceability:

```csharp
using CloudTek.Testing;

[Fact]
[UnitTest]
public void MyUnitTest() { }

[Fact]
[IntegrationTest]
public void MyIntegrationTest() { }

[Fact]
[ModuleTest]
public void MyModuleTest() { }

[Fact]
[SmokeTest]
public void MySmokeTest() { }

[Fact]
[SystemTest]
public void MySystemTest() { }
```

Run specific test categories:

```bash
dotnet test --filter "Category=UnitTests"
dotnet test --filter "Category=IntegrationTests"
```

#### TestPortProvider

Provides available ports for integration tests to avoid port conflicts:

```csharp
using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out var port);

// Service will bind to available port instead of 5000
// Port is automatically released when portScope is disposed
```

#### EnvironmentVariableScope

Scoped environment variable manipulation for tests:

```csharp
using (EnvironmentVariableScope.Create("MY_VAR", "test-value"))
{
  // MY_VAR is set to "test-value" within this scope
  Assert.Equal("test-value", Environment.GetEnvironmentVariable("MY_VAR"));
}
// MY_VAR is automatically restored to original value
```

### Hive.MicroServices

`InTestClass<T>()` and `InTestAssembly()` (namespace `Hive.MicroServices.Extensions`) scope a service's entrypoint-assembly discovery to a test class or assembly:

```csharp
var service = new MicroService("test-service")
  .InTestClass<MyTestClass>()
  .ConfigureApiPipeline(endpoints => { });
```

For TestServer hosting, `ConfigureTestHost()` lives in `Hive.MicroServices.Testing`.

### Hive.Testing (in-repo only)

Not published to NuGet; used by this repository's own test projects. Provides:

- `ShouldStart(this IMicroService, TimeSpan)` and `ShouldFailToStart(this IMicroService, TimeSpan)` (synchronous)
- `UseEmbeddedConfiguration` and `UseDefaultLoggingConfiguration` configuration helpers

```csharp
service.CancellationTokenSource.CancelAfter(1000);

// Verify service starts successfully
service.ShouldStart(TimeSpan.FromSeconds(5));

// Verify service fails to start (for negative tests)
service.ShouldFailToStart(TimeSpan.FromSeconds(5));
```

## Best Practices

### Configuration Validation

1. **Use FluentValidation for production** - Provides detailed error messages and composable validators
2. **Use `PreConfigureOptionalValidatedOptions` for optional configuration** - Allows defaults when section is missing
3. **Use `PreConfigureValidatedOptions` for required configuration** - Fails fast if section is missing or invalid
4. **Validate early** - Validate configuration before `IServiceProvider` is built to catch errors at startup

### Testing

1. **Use test attributes** (`CloudTek.Testing`) - Categorize tests for selective execution
2. **Use `TestPortProvider`** - Avoid port conflicts in parallel test execution
3. **Use `InTestClass<T>()`** - Simplify microservice testing
4. **Scope test configuration** - Use `InTestClass<T>()` to isolate test configuration

## Examples

### Example: Extension with Optional Configuration

```csharp
public class CacheExtension : MicroServiceExtension<CacheExtension>
{
  public CacheExtension(IMicroServiceCore service) : base(service)
  {
  }

  public override IServiceCollection ConfigureServices(
    IServiceCollection services,
    IMicroServiceCore microservice)
  {
    // Load optional cache configuration
    var options = services.PreConfigureOptionalValidatedOptions<CacheOptions, CacheOptionsValidator>(
      microservice.ConfigurationRoot,
      () => "Cache");

    // Use defaults if not configured
    var cacheOptions = options?.Value ?? new CacheOptions
    {
      ExpirationMinutes = 60,
      MaxSize = 1000
    };

    // Register cache service with configuration
    services.AddSingleton(cacheOptions);
    services.AddSingleton<ICacheService, CacheService>();
    return services;
  }
}

// Usage - works with or without configuration
var service = new MicroService("my-service")
  .RegisterExtension<CacheExtension>();  // Uses defaults

// OR with configuration
// appsettings.json:
// {
//   "Cache": {
//     "ExpirationMinutes": 120,
//     "MaxSize": 5000
//   }
// }
```

### Example: Integration Test with Port Provider

```csharp
using CloudTek.Testing;

[Fact]
[IntegrationTest]
public async Task GivenService_WhenStarted_ThenRespondsToRequests()
{
  // Arrange - Get unique port for this test
  using var portScope = TestPortProvider.GetAvailableServicePortScope(5000, out var port);

  var config = new ConfigurationBuilder().Build();
  var service = new MicroService("test-service")
    .InTestClass<MyIntegrationTests>()
    .ConfigureApiPipeline(endpoints => endpoints.MapGet("/health", () => "healthy"));

  service.CancellationTokenSource.CancelAfter(5000);

  // Act
  var runTask = service.RunAsync(config);
  service.ShouldStart(TimeSpan.FromSeconds(3));

  // Make HTTP request to service
  using var client = new HttpClient();
  var response = await client.GetStringAsync($"http://localhost:{port}/health");

  // Assert
  response.Should().Be("healthy");

  // Cleanup
  service.CancellationTokenSource.Cancel();
  await runTask;
}
```

## See Also

- [Hive.MicroServices](../hive.microservices/README.md) - Microservices framework
- [Hive.OpenTelemetry](../hive.opentelemetry/README.md) - OpenTelemetry integration
- [CLAUDE.md](../CLAUDE.md) - Full project documentation
