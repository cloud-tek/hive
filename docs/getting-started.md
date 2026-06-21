# Getting Started — Build Your First Hive Service

This guide walks through the minimum steps to get a Hive microservice running.
For a broader overview of the framework, see the [root README](../README.md).

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Optional: `CloudTek.Build.Tool` for full builds (see [build.md](build.md))

---

## 1. Create a new project

```bash
dotnet new web -n MyHiveService
cd MyHiveService
```

> Note: The repository does not currently publish `dotnet new` hive-* templates.
> Use `dotnet new web` (or `dotnet new worker` for background jobs) as the starting point.

---

## 2. Add NuGet references

For a REST API service:

```bash
dotnet add package Hive.MicroServices
dotnet add package Hive.MicroServices.Api
dotnet add package Hive.OpenTelemetry   # optional but recommended
```

For a background worker:

```bash
dotnet add package Hive.MicroServices
dotnet add package Hive.MicroServices.Job
```

---

## 3. Write your service

Replace the generated `Program.cs`:

```csharp
using Hive;
using Hive.MicroServices.Api;
using Hive.OpenTelemetry;

var service = new MicroService("my-first-service")
    .WithOpenTelemetry()
    .ConfigureApiPipeline(endpoints =>
    {
        endpoints.MapGet("/", () => "Hello from Hive!");
    });

await service.RunAsync();
```

---

## 4. Run it

```bash
dotnet run
```

Hive automatically registers Kubernetes-compatible health probes at:

- `GET /startup` — startup probe
- `GET /readiness` — readiness probe
- `GET /liveness` — liveness probe

---

## 5. Add configuration and services

```csharp
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var service = new MicroService("my-service")
    .WithOpenTelemetry()
    .ConfigureServices((services, configuration) =>
    {
        services.AddSingleton<IGreetingService, GreetingService>();
    })
    .ConfigureApiPipeline(endpoints =>
    {
        endpoints.MapGet("/greet", (IGreetingService greeter) =>
            Results.Ok(greeter.Greet()));
    });

await service.RunAsync(config);
```

---

## Next steps

| Topic | Link |
|---|---|
| Pipeline modes (GraphQL, gRPC, MCP, Job) | [hive.microservices README](../hive.microservices/README.md) |
| Core framework constraints & gotchas | [Hive.MicroServices README](../hive.microservices/src/Hive.MicroServices/README.md) |
| MCP server hosting | [Hive.MicroServices.Mcp README](../hive.microservices/src/Hive.MicroServices.Mcp/README.md) |
| OpenTelemetry configuration | [Hive.OpenTelemetry README](../hive.opentelemetry/README.md) |
| Writing integration tests | [Hive.MicroServices.Testing README](../hive.microservices/src/Hive.MicroServices.Testing/README.md) |
| CORS configuration | [CORS README](../hive.microservices/src/Hive.MicroServices/CORS/README.md) |
| Messaging (Wolverine / RabbitMQ) | [Hive.Messaging README](../hive.extensions/src/Hive.Messaging/README.md) |
| Azure Functions | [Hive.Functions README](../hive.functions/README.md) |
| Full documentation index | [docs/README.md](README.md) |