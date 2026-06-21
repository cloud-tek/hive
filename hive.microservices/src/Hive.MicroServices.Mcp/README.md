# Hive.MicroServices.Mcp

Model Context Protocol (MCP) server support for Hive microservices, exposing AI-callable tools, prompts, and resources over the streamable HTTP transport via `ModelContextProtocol.AspNetCore`.

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Tool Registration](#tool-registration)
- [Transport](#transport)
- [Coexistence with Custom Routes](#coexistence-with-custom-routes)
- [Prompts and Resources](#prompts-and-resources)

## Overview

`Hive.MicroServices.Mcp` activates the `Mcp` pipeline mode on a `MicroService`. The pipeline wires a standard ASP.NET Core middleware stack — routing, optional CORS, authorization — and maps the MCP streamable HTTP endpoint via `MapMcp()`. The result is a fully hosted MCP server that participates in Hive's Kubernetes probes, OpenTelemetry integration, and extension ecosystem.

Use this package when you want to expose AI-callable capabilities (tools, prompts, resources) to LLM clients such as Claude, Copilot, or any MCP-compatible host, while keeping the service inside the same Hive operational envelope as your REST or gRPC services.

**When to use:**
- You are building a standalone MCP server or a sidecar that surfaces domain operations to AI agents.
- You need tools to resolve shared application services from DI (e.g. a repository, a cache, an HTTP client).
- You want auxiliary HTTP routes (admin, webhooks) alongside the MCP endpoint without a separate service.

**Not compatible with:**
- `Hive.MicroServices.Job` — worker services have no HTTP pipeline; `MapEndpoints` on a Job host throws `ConfigurationException` at startup.

## Quick Start

```csharp
using Hive.MicroServices;
using Hive.MicroServices.Mcp;
using Hive.OpenTelemetry;

var service = new MicroService("my-mcp-server")
    .WithOpenTelemetry()
    .ConfigureServices((services, configuration) =>
    {
      services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
    })
    .ConfigureMcpPipeline(mcp =>
    {
      mcp.WithTools<WeatherForecastTool>();
    });

await service.RunAsync();
```

`ConfigureMcpPipeline` accepts an `Action<IMcpServerBuilder>` callback. Everything registered on the builder (tools, prompts, resources) is served over the MCP streamable HTTP endpoint.

## Tool Registration

Tools are plain C# classes decorated with `[McpServerToolType]`. Individual methods are decorated with `[McpServerTool]`. Parameters that are not MCP arguments are resolved from DI — the MCP SDK performs constructor and parameter injection automatically.

### Attribute-based tool

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public class WeatherForecastTool
{
  [McpServerTool(Name = "get_weather_forecast")]
  [Description("Gets the weather forecast for the next few days.")]
  public static IEnumerable<WeatherForecast> GetWeatherForecast(IWeatherForecastService service)
  {
    return service.GetWeatherForecast();
  }
}
```

`IWeatherForecastService` is injected by the MCP runtime from the DI container. No constructor is needed — the parameter is resolved per-invocation.

### Registration

```csharp
.ConfigureMcpPipeline(mcp =>
{
  mcp.WithTools<WeatherForecastTool>();
})
```

Register one tool type per `WithTools<T>()` call, or chain multiple calls:

```csharp
.ConfigureMcpPipeline(mcp =>
{
  mcp
    .WithTools<WeatherForecastTool>()
    .WithTools<InventoryTool>();
})
```

The `[McpServerTool]` attribute's `Name` property sets the tool name visible to MCP clients. `[Description]` on both the class and its methods populates the MCP schema descriptions shown to LLMs.

## Transport

The MCP server uses the **streamable HTTP transport** (`WithHttpTransport()`). The endpoint is registered at the default path `/mcp` (the `ModelContextProtocol.AspNetCore` default) via `MapMcp()` inside `UseEndpoints`.

**Middleware order applied by `ConfigureMcpPipeline`:**

```
UseRouting
  └─ UseCors          (only when a CORS extension is registered)
       └─ UseAuthorization
            └─ UseEndpoints
                 ├─ MapMcp()              ← MCP streamable HTTP endpoint
                 └─ [custom MapEndpoints routes]
```

No additional transport configuration is required. MCP clients connect to `POST /mcp` (SSE stream) on the service's configured port.

## Coexistence with Custom Routes

Use the `MapEndpoints` seam to register auxiliary HTTP routes that run **inside the same routing/CORS/authorization envelope** as the MCP endpoint, sharing the same DI container. This is the recommended approach; it replaces older workarounds using `RegisterExtension` with a manual `Configure(IApplicationBuilder)` override.

The seam is defined in `Hive.MicroServices` and is mode-agnostic. See the design rationale in [`docs/design/custom_endpoints_seam_design.md`](../../../../docs/design/custom_endpoints_seam_design.md).

### Example — admin route sharing a DI singleton with a tool

```csharp
var service = new MicroService("hive-microservices-mcp-demo")
    .WithOpenTelemetry()
    .ConfigureServices((services, configuration) =>
    {
      // Singleton shared by both the MCP tool and the admin route.
      services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
    })
    .ConfigureMcpPipeline(mcp =>
    {
      mcp.WithTools<WeatherForecastTool>();
    })
    .MapEndpoints(routes =>
    {
      // Custom admin endpoint — resolves the same IWeatherForecastService singleton.
      routes.MapGet("/admin/forecast/summary", (IWeatherForecastService svc) =>
        Results.Ok(new { count = svc.GetWeatherForecast().Count() }));
    });

await service.RunAsync();
```

`MapEndpoints` can be called before or after `ConfigureMcpPipeline` — order does not matter. Multiple `MapEndpoints` calls accumulate; all registered delegates run inside the single `UseEndpoints` block.

**Job host restriction:** calling `MapEndpoints` on a service that uses `ConfigureJobPipeline` throws `ConfigurationException` at startup with the message from `Constants.Errors.MapEndpointsJobForbidden`. This is by design — Job (worker) services have no HTTP pipeline.

## Prompts and Resources

`ModelContextProtocol` 1.4.0 supports MCP prompts and resources via attribute-based registration, symmetric with tools.

### Prompts

Decorate a class with `[McpServerPromptType]` and methods with `[McpServerPrompt]`, then register with `WithPrompts<T>()`:

```csharp
using ModelContextProtocol.Server;

[McpServerPromptType]
public class ForecastPrompts
{
  [McpServerPrompt(Name = "summarize_forecast")]
  [Description("Returns a prompt that asks the LLM to summarize the current weather forecast.")]
  public static string SummarizeForecast(IWeatherForecastService svc)
  {
    var items = svc.GetWeatherForecast();
    return $"Summarize this forecast in one sentence: {string.Join(", ", items)}";
  }
}
```

```csharp
.ConfigureMcpPipeline(mcp =>
{
  mcp
    .WithTools<WeatherForecastTool>()
    .WithPrompts<ForecastPrompts>();
})
```

### Resources

Decorate a class with `[McpServerResourceType]` and members with `[McpServerResource]`, then register with `WithResources<T>()`:

```csharp
using ModelContextProtocol.Server;

[McpServerResourceType]
public class ForecastResources
{
  [McpServerResource(Name = "current_forecast", Uri = "forecast://current")]
  [Description("The current weather forecast as a structured list.")]
  public static IEnumerable<WeatherForecast> CurrentForecast(IWeatherForecastService svc)
  {
    return svc.GetWeatherForecast();
  }
}
```

```csharp
.ConfigureMcpPipeline(mcp =>
{
  mcp
    .WithTools<WeatherForecastTool>()
    .WithResources<ForecastResources>();
})
```

DI injection into prompt and resource methods follows the same rules as tools: parameters typed as registered services are resolved from the DI container per-invocation.
