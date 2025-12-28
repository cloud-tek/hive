# Hive.OpenTelemetry Configuration Strategy

## Problem Statement

The current OpenTelemetry extension has three main configuration issues:

1. **Configuration Order**: Environment variables are read as a snapshot during MicroService construction, but IConfiguration is built later during host creation
2. **No IConfiguration Integration**: Cannot configure via appsettings.json or other IConfiguration sources
3. **Limited Flexibility**: Developers can only override defaults via lambda callbacks, not through declarative configuration

## Configuration Loading Order in Hive

Understanding the MicroService lifecycle is critical:

```
1. MicroService Constructor
   - Captures EnvironmentVariables snapshot (MicroServiceBase.cs:22-25)
   - IMicroService.EnvironmentVariables is ReadOnlyDictionary

2. Extension Constructor (.WithOpenTelemetry())
   - Adds ConfigureActions to extension
   - NO access to IConfiguration yet

3. RunAsync() → CreateHostBuilder()
   - ConfigureAppConfiguration runs (MicroService.cs:166-184)
     * Builds IConfiguration from appsettings.json, env vars, command line
     * ConfigurationRoot becomes available

4. ConfigureServices runs (MicroService.cs:190-198)
   - Extension.ConfigureActions execute HERE
   - HAS access to IConfiguration via cfg parameter
   - Services are being registered

5. Configure (pipeline) runs (MicroService.cs:199-212)
   - Application middleware configured

6. Host.RunAsync()
   - Application starts
```

**Key Insight**: `ConfigureActions` has access to both `IServiceCollection` and `IConfiguration`, but the extension constructor only has access to `IMicroService`.

## Proposed Solution: Options Pattern with Layered Configuration

### Design Principles

1. **Sane Defaults**: Framework provides sensible defaults that work out-of-the-box
2. **Declarative Configuration**: Developers can configure via appsettings.json
3. **Programmatic Overrides**: Developers can override via lambda callbacks
4. **OTEL Standard Compliance**: Support standard OpenTelemetry environment variables
5. **Hive Consistency**: Follow the same patterns as other Hive extensions (CORS, Logging)

### Configuration Model

```csharp
// hive.opentelemetry/src/Hive.OpenTelemetry/Options.cs
public class OpenTelemetryOptions
{
    public const string SectionKey = "OpenTelemetry";

    /// <summary>
    /// Resource attributes configuration
    /// </summary>
    public ResourceOptions Resource { get; set; } = new();

    /// <summary>
    /// Logging configuration
    /// </summary>
    public LoggingOptions Logging { get; set; } = new();

    /// <summary>
    /// Tracing configuration
    /// </summary>
    public TracingOptions Tracing { get; set; } = new();

    /// <summary>
    /// Metrics configuration
    /// </summary>
    public MetricsOptions Metrics { get; set; } = new();

    /// <summary>
    /// OTLP exporter configuration
    /// </summary>
    public OtlpExporterOptions Otlp { get; set; } = new();
}

public class ResourceOptions
{
    /// <summary>
    /// Service namespace (optional)
    /// </summary>
    public string? ServiceNamespace { get; set; }

    /// <summary>
    /// Service version (optional)
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Additional resource attributes
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } = new();
}

public class LoggingOptions
{
    /// <summary>
    /// Enable console exporter (default: true)
    /// </summary>
    public bool EnableConsoleExporter { get; set; } = true;

    /// <summary>
    /// Enable OTLP exporter (default: false, enabled if Otlp.Endpoint is set)
    /// </summary>
    public bool EnableOtlpExporter { get; set; } = false;
}

public class TracingOptions
{
    /// <summary>
    /// Enable ASP.NET Core instrumentation (default: true)
    /// </summary>
    public bool EnableAspNetCoreInstrumentation { get; set; } = true;

    /// <summary>
    /// Enable HTTP Client instrumentation (default: true)
    /// </summary>
    public bool EnableHttpClientInstrumentation { get; set; } = true;

    /// <summary>
    /// Enable OTLP exporter (default: false, enabled if Otlp.Endpoint is set)
    /// </summary>
    public bool EnableOtlpExporter { get; set; } = false;
}

public class MetricsOptions
{
    /// <summary>
    /// Enable ASP.NET Core instrumentation (default: true)
    /// </summary>
    public bool EnableAspNetCoreInstrumentation { get; set; } = true;

    /// <summary>
    /// Enable HTTP Client instrumentation (default: true)
    /// </summary>
    public bool EnableHttpClientInstrumentation { get; set; } = true;

    /// <summary>
    /// Enable Runtime instrumentation (default: true)
    /// </summary>
    public bool EnableRuntimeInstrumentation { get; set; } = true;

    /// <summary>
    /// Enable OTLP exporter (default: false, enabled if Otlp.Endpoint is set)
    /// </summary>
    public bool EnableOtlpExporter { get; set; } = false;
}

public class OtlpExporterOptions
{
    /// <summary>
    /// OTLP endpoint (e.g., "http://localhost:4317")
    /// Falls back to OTEL_EXPORTER_OTLP_ENDPOINT environment variable
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Protocol (default: Grpc)
    /// </summary>
    public OtlpExportProtocol Protocol { get; set; } = OtlpExportProtocol.Grpc;

    /// <summary>
    /// Headers (e.g., for authentication)
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Timeout in milliseconds (default: 10000)
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 10000;
}
```

### Extension Implementation

```csharp
// hive.opentelemetry/src/Hive.OpenTelemetry/Extension.cs
public class Extension : MicroServiceExtension
{
    private readonly Action<LoggerProviderBuilder>? loggingOverride;
    private readonly Action<TracerProviderBuilder>? tracingOverride;
    private readonly Action<MeterProviderBuilder>? metricsOverride;

    public Extension(
        IMicroService service,
        Action<LoggerProviderBuilder>? logging = null,
        Action<TracerProviderBuilder>? tracing = null,
        Action<MeterProviderBuilder>? metrics = null)
        : base(service)
    {
        loggingOverride = logging;
        tracingOverride = tracing;
        metricsOverride = metrics;

        ConfigureActions.Add((svc, cfg) =>
        {
            // 1. Load options from IConfiguration with defaults
            var options = LoadOptionsFromConfiguration(svc, cfg);

            // 2. Resolve OTLP endpoint with fallback chain
            var otlpEndpoint = ResolveOtlpEndpoint(options, service);

            // 3. Configure OpenTelemetry
            svc.AddOpenTelemetry()
                .ConfigureResource(resource => ConfigureResource(resource, service, options))
                .WithLogging(builder => ConfigureLogging(builder, options, otlpEndpoint))
                .WithTracing(builder => ConfigureTracing(builder, options, otlpEndpoint))
                .WithMetrics(builder => ConfigureMetrics(builder, options, otlpEndpoint));
        });
    }

    private OpenTelemetryOptions LoadOptionsFromConfiguration(
        IServiceCollection services,
        IConfiguration configuration)
    {
        // Try to load from IConfiguration, fall back to defaults
        var section = configuration.GetSection(OpenTelemetryOptions.SectionKey);
        var options = new OpenTelemetryOptions();

        if (section.Exists())
        {
            section.Bind(options);
        }

        // Register options for DI
        services.Configure<OpenTelemetryOptions>(section);

        return options;
    }

    private string? ResolveOtlpEndpoint(OpenTelemetryOptions options, IMicroService service)
    {
        // Priority order:
        // 1. IConfiguration (options.Otlp.Endpoint)
        // 2. Environment variable OTEL_EXPORTER_OTLP_ENDPOINT
        // 3. null (no OTLP export)

        if (!string.IsNullOrWhiteSpace(options.Otlp.Endpoint))
            return options.Otlp.Endpoint;

        if (service.EnvironmentVariables.TryGetValue(
            Constants.Environment.OtelExporterOtlpEndpoint,
            out var envEndpoint))
            return envEndpoint;

        return null;
    }

    private ResourceBuilder ConfigureResource(
        ResourceBuilder resource,
        IMicroService service,
        OpenTelemetryOptions options)
    {
        resource.AddService(
            serviceName: service.Name,
            serviceNamespace: options.Resource.ServiceNamespace,
            serviceInstanceId: service.Id,
            serviceVersion: options.Resource.ServiceVersion,
            autoGenerateServiceInstanceId: false);

        // Add custom attributes from configuration
        foreach (var attr in options.Resource.Attributes)
        {
            resource.AddAttributes(new[] {
                new KeyValuePair<string, object>(attr.Key, attr.Value)
            });
        }

        return resource;
    }

    private void ConfigureLogging(
        LoggerProviderBuilder builder,
        OpenTelemetryOptions options,
        string? otlpEndpoint)
    {
        // If developer provided override, use it exclusively
        if (loggingOverride != null)
        {
            loggingOverride(builder);
            return;
        }

        // Otherwise use configuration-driven approach
        if (options.Logging.EnableConsoleExporter)
        {
            builder.AddConsoleExporter();
        }

        if (ShouldEnableOtlpExporter(options.Logging.EnableOtlpExporter, otlpEndpoint))
        {
            builder.AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(otlpEndpoint!);
                otlp.Protocol = options.Otlp.Protocol;
                otlp.TimeoutMilliseconds = options.Otlp.TimeoutMilliseconds;

                foreach (var header in options.Otlp.Headers)
                {
                    otlp.Headers = $"{header.Key}={header.Value}";
                }
            });
        }
    }

    private void ConfigureTracing(
        TracerProviderBuilder builder,
        OpenTelemetryOptions options,
        string? otlpEndpoint)
    {
        // If developer provided override, use it exclusively
        if (tracingOverride != null)
        {
            tracingOverride(builder);
            return;
        }

        // Otherwise use configuration-driven approach
        if (options.Tracing.EnableAspNetCoreInstrumentation)
        {
            builder.AddAspNetCoreInstrumentation();
        }

        if (options.Tracing.EnableHttpClientInstrumentation)
        {
            builder.AddHttpClientInstrumentation();
        }

        if (ShouldEnableOtlpExporter(options.Tracing.EnableOtlpExporter, otlpEndpoint))
        {
            builder.AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(otlpEndpoint!);
                otlp.Protocol = options.Otlp.Protocol;
                otlp.TimeoutMilliseconds = options.Otlp.TimeoutMilliseconds;

                foreach (var header in options.Otlp.Headers)
                {
                    otlp.Headers = $"{header.Key}={header.Value}";
                }
            });
        }
    }

    private void ConfigureMetrics(
        MeterProviderBuilder builder,
        OpenTelemetryOptions options,
        string? otlpEndpoint)
    {
        // If developer provided override, use it exclusively
        if (metricsOverride != null)
        {
            metricsOverride(builder);
            return;
        }

        // Otherwise use configuration-driven approach
        if (options.Metrics.EnableAspNetCoreInstrumentation)
        {
            builder.AddAspNetCoreInstrumentation();
        }

        if (options.Metrics.EnableHttpClientInstrumentation)
        {
            builder.AddHttpClientInstrumentation();
        }

        if (options.Metrics.EnableRuntimeInstrumentation)
        {
            builder.AddRuntimeInstrumentation();
        }

        if (ShouldEnableOtlpExporter(options.Metrics.EnableOtlpExporter, otlpEndpoint))
        {
            builder.AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(otlpEndpoint!);
                otlp.Protocol = options.Otlp.Protocol;
                otlp.TimeoutMilliseconds = options.Otlp.TimeoutMilliseconds;

                foreach (var header in options.Otlp.Headers)
                {
                    otlp.Headers = $"{header.Key}={header.Value}";
                }
            });
        }
    }

    private bool ShouldEnableOtlpExporter(bool explicitlyEnabled, string? endpoint)
    {
        // Enable OTLP exporter if:
        // 1. Explicitly enabled in configuration, OR
        // 2. Endpoint is configured (implicit enable)
        return explicitlyEnabled || !string.IsNullOrWhiteSpace(endpoint);
    }
}
```

### Usage Examples

#### Example 1: Zero Configuration (Defaults)

```csharp
var service = new MicroService("my-service")
    .WithOpenTelemetry()  // Uses all defaults: console exporter, all instrumentation
    .ConfigureApiPipeline(app => { });
```

**Behavior:**
- ✓ Console exporter for logs
- ✓ ASP.NET Core + HTTP Client instrumentation for traces
- ✓ ASP.NET Core + HTTP Client + Runtime instrumentation for metrics
- ✗ No OTLP export (no endpoint configured)

#### Example 2: Configuration via appsettings.json

```json
{
  "OpenTelemetry": {
    "Resource": {
      "ServiceNamespace": "my-company",
      "ServiceVersion": "1.0.0",
      "Attributes": {
        "deployment.environment": "production",
        "team": "platform"
      }
    },
    "Otlp": {
      "Endpoint": "http://otel-collector:4317",
      "Protocol": "Grpc",
      "TimeoutMilliseconds": 5000,
      "Headers": {
        "x-api-key": "secret"
      }
    },
    "Logging": {
      "EnableConsoleExporter": true,
      "EnableOtlpExporter": true
    },
    "Tracing": {
      "EnableAspNetCoreInstrumentation": true,
      "EnableHttpClientInstrumentation": true,
      "EnableOtlpExporter": true
    },
    "Metrics": {
      "EnableAspNetCoreInstrumentation": true,
      "EnableHttpClientInstrumentation": true,
      "EnableRuntimeInstrumentation": false,
      "EnableOtlpExporter": true
    }
  }
}
```

```csharp
var service = new MicroService("my-service")
    .WithOpenTelemetry()  // Reads from appsettings.json
    .ConfigureApiPipeline(app => { });
```

**Behavior:**
- ✓ Console + OTLP exporters for logs
- ✓ OTLP exporter for traces with custom headers
- ✓ OTLP exporter for metrics (runtime instrumentation disabled)
- ✓ Custom resource attributes

#### Example 3: Environment Variable Override

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

```csharp
var service = new MicroService("my-service")
    .WithOpenTelemetry()
    .ConfigureApiPipeline(app => { });
```

**Behavior:**
- ✓ OTLP endpoint from environment variable
- ✓ All default instrumentations enabled

#### Example 4: Programmatic Override (Lambda)

```csharp
var service = new MicroService("my-service")
    .WithOpenTelemetry(
        logging: log =>
        {
            // Complete control - ignores appsettings.json
            log.AddOtlpExporter(o => o.Endpoint = new Uri("http://custom:4317"));
        },
        tracing: trace =>
        {
            // Custom instrumentation
            trace.AddAspNetCoreInstrumentation();
            trace.AddSource("MyApp.*");
        }
    )
    .ConfigureApiPipeline(app => { });
```

**Behavior:**
- ✓ Lambda overrides take complete control
- ✗ appsettings.json configuration ignored when lambda provided

#### Example 5: Hybrid Approach

```json
{
  "OpenTelemetry": {
    "Otlp": {
      "Endpoint": "http://otel-collector:4317"
    },
    "Metrics": {
      "EnableRuntimeInstrumentation": false
    }
  }
}
```

```csharp
var service = new MicroService("my-service")
    .WithOpenTelemetry(
        // Only override tracing, let logging & metrics use appsettings.json
        tracing: trace =>
        {
            trace.AddAspNetCoreInstrumentation();
            trace.AddSource("MyCustomSource");
        }
    )
    .ConfigureApiPipeline(app => { });
```

**Behavior:**
- ✓ Tracing: Custom lambda configuration
- ✓ Logging: Uses appsettings.json (OTLP endpoint from config)
- ✓ Metrics: Uses appsettings.json (runtime instrumentation disabled)

### Configuration Priority

For each signal (Logging, Tracing, Metrics):

```
1. Lambda Override (if provided)
   → Completely bypasses appsettings.json and defaults

2. appsettings.json Configuration
   → Merges with defaults
   → Can enable/disable individual features

3. Framework Defaults
   → Sensible defaults for quick start
```

For OTLP Endpoint specifically:

```
1. appsettings.json: OpenTelemetry.Otlp.Endpoint

2. Environment Variable: OTEL_EXPORTER_OTLP_ENDPOINT

3. null (no OTLP export)
```

### Benefits

1. **Zero Configuration Works**: Developers can call `.WithOpenTelemetry()` with no parameters and get sensible defaults
2. **Declarative Configuration**: Infrastructure teams can configure via appsettings.json without code changes
3. **Environment-Aware**: Different configurations for dev/staging/prod via appsettings.{Environment}.json
4. **OTEL Standard Compliance**: Respects standard `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable
5. **Flexible Overrides**: Developers can programmatically override when needed
6. **Consistent with Hive Patterns**: Follows the same patterns as CORS and other extensions

### Migration Path

For existing code using the current API:

**Before:**
```csharp
service.WithOpenTelemetry()
```

**After (no changes required):**
```csharp
service.WithOpenTelemetry()  // Still works, same defaults
```

The new API is **backward compatible** - existing code continues to work with the same behavior.

## References

- [OTLP Exporter Configuration | OpenTelemetry](https://opentelemetry.io/docs/languages/sdk-configuration/otlp-exporter/)
- [OTLP Exporter for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md)
- [.NET Observability with OpenTelemetry - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel)
- [OpenTelemetry .NET Configuration](https://opentelemetry.io/docs/zero-code/dotnet/configuration/)
