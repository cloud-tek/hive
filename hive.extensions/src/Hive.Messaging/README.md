# Hive.Messaging

Opinionated messaging extension for Hive microservices, providing a fluent builder API for message handling and sending built on [Wolverine](https://wolverinefx.net/).

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Builder API](#builder-api)
- [Transport Selection](#transport-selection)
- [Handling Configuration](#handling-configuration)
- [Sending Configuration](#sending-configuration)
- [Serialization](#serialization)
- [Configuration](#configuration)
- [Named Brokers](#named-brokers)
- [Handler Pipeline](#handler-pipeline)
- [Readiness Gating](#readiness-gating)
- [Telemetry](#telemetry)
- [Distributed Tracing](#distributed-tracing)
- [Advanced Configuration](#advanced-configuration)
- [Configuration Reference](#configuration-reference)
- [Related Projects](#related-projects)

## Overview

Hive.Messaging wraps Wolverine with an opinionated configuration layer that includes:

- Fluent builder API for declaring queue listeners and publish routes
- Pluggable transport abstraction (`IMessagingTransportProvider`) with RabbitMQ as the first-class transport
- Readiness gating — messages are rejected until the service signals readiness via Kubernetes probes
- Handler telemetry — duration, count, and error metrics per message type
- Send telemetry — send counts and errors with distributed trace context
- Automatic health checks registered by the transport provider
- `IActivitySourceProvider` integration — Wolverine traces auto-discovered by `Hive.OpenTelemetry`
- Named broker support for multi-broker topologies
- Escape hatch for direct Wolverine configuration

Two hosting modes are supported:

| Mode | Extension Method | Builder Type | Capabilities |
|------|-----------------|-------------|--------------|
| Full (handling + sending) | `IMicroService.WithMessaging()` | `HiveMessagingBuilder` | Queue listeners, publish routes, middleware, health checks |
| Send-only | `IMicroServiceCore.WithMessaging()` | `HiveMessagingSendBuilder` | Publish routes only (for Azure Functions, console apps) |

## Quick Start

### Send-Only (API publishing messages)

```csharp
using Hive.Messaging;
using Hive.Messaging.RabbitMq;

var service = new MicroService("order-api")
    .WithMessaging(builder => builder
        .UseRabbitMq()
        .WithSending(s => s
            .Publish<OrderCreatedEvent>()
            .ToQueue("q.orders.created")))
    .ConfigureApiPipeline(app =>
    {
        app.MapPost("/orders", async (IMessageBus bus, CreateOrderRequest request) =>
        {
            // Process order...
            await bus.PublishAsync(new OrderCreatedEvent(request.OrderId));
            return Results.Accepted();
        });
    });

await service.RunAsync();
```

### Full (handling + sending)

```csharp
using Hive.Messaging;
using Hive.Messaging.RabbitMq;

var service = new MicroService("order-processor")
    .WithOpenTelemetry()
    .WithMessaging(builder => builder
        .UseRabbitMq()
        .WithHandling(h => h
            .ListenToQueue("q.orders.created")
            .Prefetch(10)
            .ListenerCount(4))
        .WithSending(s => s
            .Publish<OrderProcessedEvent>()
            .ToExchange("ex.orders.processed")))
    .ConfigureDefaultServicePipeline();

await service.RunAsync();
```

Wolverine discovers handlers by convention. Create a handler class with a `Handle` method:

```csharp
public static class OrderCreatedEventHandler
{
    public static async Task Handle(OrderCreatedEvent message, IMessageBus bus, CancellationToken ct)
    {
        // Process the order...
        await bus.PublishAsync(new OrderProcessedEvent(message.OrderId));
    }
}
```

## Builder API

### HiveMessagingBuilder (full mode)

Available when calling `IMicroService.WithMessaging()`:

| Method | Description |
|--------|-------------|
| `UseRabbitMq()` | Selects RabbitMQ as the transport |
| `UseInMemoryTransport()` | Selects the in-memory transport (Wolverine built-in) |
| `WithHandling(Action<MessageHandlerBuilder>)` | Configures queue listeners |
| `WithSending(Action<MessageSenderBuilder>)` | Configures publish/send routes |
| `WithSerialization(Action<SerializationBuilder>)` | Configures message serialization |
| `ConfigureWolverine(Action<WolverineOptions>)` | Direct access to Wolverine options |

### HiveMessagingSendBuilder (send-only mode)

Available when calling `IMicroServiceCore.WithMessaging()`:

| Method | Description |
|--------|-------------|
| `UseRabbitMq()` | Selects RabbitMQ as the transport |
| `UseInMemoryTransport()` | Selects the in-memory transport |
| `WithSending(Action<MessageSenderBuilder>)` | Configures publish/send routes |
| `WithSerialization(Action<SerializationBuilder>)` | Configures message serialization |
| `ConfigureWolverine(Action<WolverineOptions>)` | Direct access to Wolverine options |

> **Note:** `WithHandling()` is intentionally absent from `HiveMessagingSendBuilder`. This prevents non-ASP.NET hosts from registering queue listeners that depend on the readiness probe infrastructure.

## Transport Selection

Transports are selected explicitly — there is no auto-discovery or assembly scanning.

### RabbitMQ

Requires the `Hive.Messaging.RabbitMq` package.

```csharp
// Read connection details from IConfiguration (Hive:Messaging:RabbitMq:ConnectionUri)
builder.UseRabbitMq()

// Explicit connection URI
builder.UseRabbitMq("amqp://guest:guest@localhost:5672/")

// Via builder for additional options
builder.UseRabbitMq(rmq => rmq
    .ConnectionUri("amqp://localhost:5672/")
    .AutoProvision())
```

### In-Memory

Uses Wolverine's built-in in-memory transport. Useful for testing and single-process scenarios:

```csharp
builder.UseInMemoryTransport()
```

### Custom Transports

Implement `IMessagingTransportProvider` to add support for other brokers (Kafka, Azure Service Bus, etc.). The provider is responsible for:

- Configuring the Wolverine transport
- Registering queue listeners and publish routes
- Registering health checks
- Validating configuration

## Handling Configuration

Configure queue listeners via `WithHandling()`:

```csharp
builder.WithHandling(h => h
    .ListenToQueue("q.orders")
    .Prefetch(20)
    .ListenerCount(4))
```

### Listener Options

| Method | Description | Default |
|--------|-------------|---------|
| `ListenToQueue(name)` | Queue to consume from | *(required)* |
| `.OnBroker(name)` | Named broker for this listener | Default broker |
| `.Prefetch(count)` | Messages to prefetch from the broker | Transport default |
| `.ListenerCount(count)` | Number of concurrent listeners | Transport default |
| `.Sequential()` | Single-threaded processing (Prefetch=1, ListenerCount=1) | — |

### Multiple Queues

Chain multiple `ListenToQueue` calls:

```csharp
builder.WithHandling(h =>
{
    h.ListenToQueue("q.orders.created").Prefetch(10);
    h.ListenToQueue("q.orders.cancelled").Sequential();
})
```

## Sending Configuration

Configure publish/send routes via `WithSending()`:

```csharp
builder.WithSending(s => s
    .Publish<OrderCreatedEvent>()
    .ToQueue("q.orders.created"))
```

### Routing Options

| Method | Description |
|--------|-------------|
| `Publish<T>()` | Begins routing configuration for message type `T` |
| `.OnBroker(name)` | Routes to a named broker |
| `.ToQueue(name)` | Routes to a queue |
| `.ToExchange(name)` | Routes to an exchange (fan-out, topic routing) |

### Multiple Routes

```csharp
builder.WithSending(s =>
{
    s.Publish<OrderCreatedEvent>().ToQueue("q.orders.created");
    s.Publish<AuditEvent>().ToExchange("ex.audit");
    s.Publish<NotificationEvent>().OnBroker("notifications").ToQueue("q.notifications");
})
```

## Serialization

Configure message serialization format:

```csharp
builder.WithSerialization(s => s.UseSystemTextJson())
```

| Format | Method | Status |
|--------|--------|--------|
| System.Text.Json | `UseSystemTextJson()` | Default |
| Newtonsoft.Json | — | Planned |
| MessagePack | — | Planned |
| Protobuf | — | Planned |

## Configuration

Hive.Messaging supports configuration via both `IConfiguration` (JSON/environment variables) and the fluent builder API. The fluent API takes precedence.

### JSON Configuration

```json
{
  "Hive": {
    "Messaging": {
      "Transport": "RabbitMQ",
      "Serialization": "SystemTextJson",
      "RabbitMq": {
        "ConnectionUri": "amqp://guest:guest@localhost:5672/",
        "AutoProvision": true
      },
      "Handling": {
        "PrefetchCount": 10,
        "ListenerCount": 4
      }
    }
  }
}
```

### Environment Variables (via Aspire or Kubernetes)

```bash
Hive__Messaging__Transport=RabbitMQ
Hive__Messaging__RabbitMq__ConnectionUri=amqp://guest:guest@localhost:5672/
Hive__Messaging__RabbitMq__AutoProvision=true
```

### Fluent API

```csharp
builder
    .UseRabbitMq("amqp://guest:guest@localhost:5672/")
    .WithHandling(h => h.ListenToQueue("q.orders").Prefetch(10).ListenerCount(4))
    .WithSending(s => s.Publish<OrderEvent>().ToQueue("q.orders"))
```

Per-listener fluent overrides (e.g., `Prefetch(20)`) take precedence over global `Handling.PrefetchCount` from configuration.

## Named Brokers

For multi-broker topologies, configure named brokers in JSON and reference them via `.OnBroker()`:

### Configuration

```json
{
  "Hive": {
    "Messaging": {
      "Transport": "RabbitMQ",
      "RabbitMq": {
        "ConnectionUri": "amqp://primary-broker:5672/"
      },
      "NamedBrokers": {
        "analytics": {
          "RabbitMq": {
            "ConnectionUri": "amqp://analytics-broker:5672/"
          }
        }
      }
    }
  }
}
```

### Usage

```csharp
builder
    .UseRabbitMq()
    .WithSending(s =>
    {
        // Default broker
        s.Publish<OrderEvent>().ToQueue("q.orders");
        // Named broker
        s.Publish<AnalyticsEvent>().OnBroker("analytics").ToQueue("q.analytics");
    })
    .WithHandling(h =>
    {
        h.ListenToQueue("q.orders");
        h.ListenToQueue("q.analytics-replies").OnBroker("analytics");
    })
```

Each named broker gets its own health check registered as `rabbitmq:{brokerName}`.

## Handler Pipeline

Every incoming message passes through the middleware pipeline before reaching the handler:

```mermaid
sequenceDiagram
    participant Broker as Message Broker
    participant Wolverine as Wolverine Runtime
    participant Readiness as ReadinessMiddleware
    participant Telemetry as MessageHandlerMiddleware
    participant Handler as Your Handler

    Broker->>Wolverine: Deliver message
    Wolverine->>Readiness: Before()
    alt Service not ready
        Readiness-->>Wolverine: throw ServiceNotReadyException
        Wolverine->>Wolverine: PauseThenRequeue (5s)
        Wolverine->>Broker: Requeue message
    else Service ready
        Readiness->>Telemetry: Before()
        Telemetry->>Telemetry: Start stopwatch, capture tags
        Telemetry->>Handler: Handle(message)
        alt Success
            Handler-->>Telemetry: Return
            Telemetry->>Telemetry: Record duration + count
        else Exception
            Handler-->>Telemetry: Throw
            Telemetry->>Telemetry: Record duration + error
        end
        Telemetry-->>Wolverine: Finally()
    end
```

## Readiness Gating

The `ReadinessMiddleware` prevents message processing until the service is fully ready. This is critical for Kubernetes deployments where the service may receive messages before the readiness probe passes.

When `IMicroService.IsReady` is `false`:
1. The middleware throws `ServiceNotReadyException`
2. The `hive.messaging.gate.nacked` counter is incremented
3. Wolverine's error policy pauses for 5 seconds, then requeues the message
4. No messages are lost — they are retried once the service is ready

This middleware is only registered in full mode (`IMicroService.WithMessaging()`), not in send-only mode.

## Telemetry

Hive.Messaging emits metrics via `System.Diagnostics.Metrics` under the meter name `Hive.Messaging`.

### Instruments

| Instrument | Type | Unit | Description |
|------------|------|------|-------------|
| `hive.messaging.handler.duration` | Histogram | ms | Handler execution duration |
| `hive.messaging.handler.count` | Counter | — | Successfully handled messages |
| `hive.messaging.handler.errors` | Counter | — | Failed handler executions |
| `hive.messaging.send.count` | Counter | — | Messages sent/published |
| `hive.messaging.send.errors` | Counter | — | Failed send attempts |
| `hive.messaging.gate.nacked` | Counter | — | Messages rejected by readiness gate |

### Tag Dimensions

**Handler metrics** (`handler.duration`, `handler.count`, `handler.errors`):

| Tag | Description |
|-----|-------------|
| `messaging.message.type` | Message type name |
| `messaging.source` | Source queue/destination URI |
| `error.type` | Exception type name (errors only) |

**Send metrics** (`send.count`, `send.errors`):

| Tag | Description |
|-----|-------------|
| `messaging.message.type` | Message type name |
| `messaging.destination` | Destination topic (broadcast only) |
| `error.type` | Exception type name (errors only) |

## Distributed Tracing

Hive.Messaging implements `IActivitySourceProvider` and exposes the `"Wolverine"` activity source. When `Hive.OpenTelemetry` is also registered, it auto-discovers this source and subscribes to it without manual wiring:

```csharp
var service = new MicroService("my-service")
    .WithOpenTelemetry()   // Auto-subscribes to Wolverine traces
    .WithMessaging(builder => builder
        .UseRabbitMq()
        .WithHandling(h => h.ListenToQueue("q.orders")));
```

No `AddSource("Wolverine")` call is needed. The `IActivitySourceProvider` cross-extension discovery handles it automatically.

For custom activity sources in your handlers:

```csharp
var service = new MicroService("my-service")
    .WithOpenTelemetry(additionalActivitySources: ["MyApp.Handlers"])
    .WithMessaging(builder => builder.UseRabbitMq()
        .WithHandling(h => h.ListenToQueue("q.orders")));
```

## Advanced Configuration

The `ConfigureWolverine()` escape hatch provides direct access to `WolverineOptions` for scenarios not covered by the fluent builder.

### Parallelism and Concurrency

```csharp
builder.ConfigureWolverine(opts =>
{
    opts.Policies.MaximumParallelMessages(50);
})
```

### Circuit Breaker per Listener

```csharp
builder.ConfigureWolverine(opts =>
{
    opts.ListenToRabbitQueue("q.orders")
        .CircuitBreaker(cb =>
        {
            cb.MinimumThreshold = 10;
            cb.PauseTime = TimeSpan.FromSeconds(30);
            cb.TrackingPeriod = TimeSpan.FromMinutes(1);
            cb.FailurePercentageThreshold = 20;
        });
})
```

### Custom Error Policies

```csharp
builder.ConfigureWolverine(opts =>
{
    opts.Policies.OnException<TimeoutException>()
        .RetryWithCooldown(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(30));

    opts.Policies.OnException<InvalidOperationException>()
        .MoveToErrorQueue();
})
```

### Execution Timeout

```csharp
builder.ConfigureWolverine(opts =>
{
    opts.Policies.ExecutionTimeout(TimeSpan.FromMinutes(5));
})
```

> **Note:** Escape hatch actions are applied after all builder configuration, so they can override anything set by the fluent API.

## Configuration Reference

### MessagingOptions (`Hive:Messaging`)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Transport` | `MessagingTransport` | `InMemory` | Transport: `RabbitMQ` or `InMemory` |
| `Serialization` | `MessagingSerialization` | `SystemTextJson` | Serialization format |
| `Handling` | `HandlingOptions` | *(see below)* | Handler defaults |
| `NamedBrokers` | `Dictionary<string, NamedBrokerOptions>` | `{}` | Named broker configurations |

### HandlingOptions (`Hive:Messaging:Handling`)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `PrefetchCount` | `int?` | `null` | Global default prefetch count |
| `ListenerCount` | `int?` | `null` | Global default listener count |

### RabbitMqOptions (`Hive:Messaging:RabbitMq`)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConnectionUri` | `string?` | `null` | AMQP connection URI (required when Transport=RabbitMQ) |
| `AutoProvision` | `bool` | `false` | Auto-create queues/exchanges on startup |

## Related Projects

- [Hive.Messaging.RabbitMq](../Hive.Messaging.RabbitMq/) — RabbitMQ transport provider
- [Hive.OpenTelemetry](../../../hive.opentelemetry/src/Hive.OpenTelemetry/) — OpenTelemetry integration (auto-discovers messaging traces)
- [Design Document](../../../docs/messaging.md) — Internal architecture decisions and design rationale
