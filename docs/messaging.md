# Hive.Messaging - Messaging Extension Design

**Status**: Draft Design Document
**Date**: 2026-02-23

---

## Executive Summary

This document outlines the design for `Hive.Messaging`, a messaging extension for the Hive framework built on [Wolverine](https://github.com/JasperFx/wolverine). The extension provides Hive-style integration for message-based communication with RabbitMQ as the initial transport, pluggable serialization, and EF Core-backed inbox/outbox durability.

### Key Design Principles

1. **Hive Extension Pattern** - Follow the established `MicroServiceExtension<T>` + `Startup.cs` + builder pattern
2. **Wolverine as Engine** - Leverage Wolverine's handler discovery, transport abstraction, and durability; don't reinvent
3. **Separate Sender/Handler Concerns** - Distinct builder APIs for message publishing and message handling within a single assembly
4. **Observable by Default** - Generic handler base with built-in error handling and telemetry (metrics, traces, debug logs)
5. **Serialization Flexibility** - JSON mandatory, MessagePack and Protobuf as opt-in alternatives
6. **Multi-Transport via Wolverine** - RabbitMQ first, with Wolverine's transport system enabling Azure Service Bus, Kafka, etc. later

---

## 1. Repository Structure

Following the repository policies, `Hive.Messaging` lives in the existing `hive.extensions` module:

```
hive.extensions/
├── src/
│   ├── Hive.HTTP/
│   ├── Hive.HTTP.Testing/
│   ├── Hive.Messaging/
│   │   ├── Hive.Messaging.csproj
│   │   ├── MessagingExtensionBase.cs
│   │   ├── MessagingExtension.cs
│   │   ├── MessagingSendExtension.cs
│   │   ├── Startup.cs
│   │   ├── HiveMessagingSendBuilder.cs
│   │   ├── HiveMessagingBuilder.cs
│   │   ├── Sending/
│   │   │   └── MessageSenderBuilder.cs
│   │   ├── Handling/
│   │   │   ├── MessageHandlerBuilder.cs
│   │   │   └── HiveMessageHandler.cs
│   │   ├── Serialization/
│   │   │   └── SerializationBuilder.cs
│   │   ├── Transport/
│   │   │   └── RabbitMqTransportBuilder.cs
│   │   ├── Configuration/
│   │   │   ├── MessagingOptions.cs
│   │   │   ├── MessagingTransport.cs
│   │   │   ├── MessagingSerialization.cs
│   │   │   ├── HandlingOptions.cs
│   │   │   ├── RabbitMqOptions.cs
│   │   │   └── MessagingOptionsValidator.cs
│   │   ├── Middleware/
│   │   │   ├── ReadinessMiddleware.cs
│   │   │   ├── ServiceNotReadyException.cs
│   │   │   └── MessageHandlerMiddleware.cs
│   │   └── Telemetry/
│   │       ├── MessagingMeter.cs
│   │       ├── TelemetryMessageBus.cs
│   │       └── WolverineSendActivityListener.cs
│   ├── Hive.Messaging.EntityFrameworkCore/
│   │   ├── Hive.Messaging.EntityFrameworkCore.csproj
│   │   ├── DurabilityBuilder.cs
│   │   ├── DurabilityOptions.cs
│   │   └── MessagingBuilderExtensions.cs
│   ├── Hive.Messaging.Testing/
│   │   ├── Hive.Messaging.Testing.csproj
│   │   └── IMicroServiceMessagingTestExtensions.cs
└── tests/
    ├── Hive.HTTP.Tests/
    └── Hive.Messaging.Tests/
        ├── Hive.Messaging.Tests.csproj
        └── ...
```

### Module Dependencies

```
Hive.Abstractions (foundation)
└── Hive.MicroServices
    ├── Hive.Messaging
    │   ├── WolverineFx
    │   ├── WolverineFx.RabbitMQ
    │   └── FluentValidation
    └── Hive.Messaging.EntityFrameworkCore
        ├── Hive.Messaging
        └── WolverineFx.EntityFrameworkCore
```

### NuGet Packages

| Package | Assembly | Purpose |
|---------|----------|---------|
| `Hive.Messaging` | `Hive.Messaging` | Core messaging extension |
| `Hive.Messaging.EntityFrameworkCore` | `Hive.Messaging.EntityFrameworkCore` | EF Core inbox/outbox durability |
| `Hive.Messaging.Testing` | `Hive.Messaging.Testing` | In-memory transport test helpers |

### Third-Party Dependencies

| Package | Referenced By | Purpose |
|---------|--------------|---------|
| `WolverineFx` | `Hive.Messaging` | Core messaging framework |
| `WolverineFx.RabbitMQ` | `Hive.Messaging` | RabbitMQ transport |
| `WolverineFx.EntityFrameworkCore` | `Hive.Messaging.EntityFrameworkCore` | EF Core inbox/outbox persistence |
| `WolverineFx.MessagePack` | `Hive.Messaging` (optional) | MessagePack serialization |
| `WolverineFx.Protobuf` | `Hive.Messaging` (optional) | Protobuf serialization |
| `FluentValidation` | `Hive.Messaging` | Configuration validation |

---

## 2. Public API Design

### 2.1 Registration (Startup.cs)

Following the Hive.HTTP pattern with dual `IMicroService` / `IMicroServiceCore` overloads:

```csharp
// Minimal registration — single broker
var service = new MicroService("order-service")
    .WithMessaging(messaging => messaging
        .UseRabbitMq("amqp://localhost"))
    .ConfigureApiPipeline(app => { /* ... */ });

// Full configuration — single broker
var service = new MicroService("order-service")
    .WithMessaging(messaging => messaging
        .UseRabbitMq(rabbit => rabbit
            .ConnectionUri("amqp://localhost")
            .AutoProvision())
        .WithSerialization(s => s.UseSystemTextJson())
        .WithSending(sending => sending
            .Publish<OrderPlaced>().ToExchange("orders")
            .Publish<PaymentRequested>().ToQueue("payments"))
        .WithHandling(handling => handling
            .ListenToQueue("orders")                    // uses global defaults from Hive:Messaging:Handling
            .ListenToQueue("payments").Prefetch(1))     // override: sequential processing for this queue
        .WithDurability(d => d                          // Requires Hive.Messaging.EntityFrameworkCore package
            .UseEntityFrameworkCore<AppDbContext>()
            .UseDurableInbox()
            .UseDurableOutbox()))
    .ConfigureApiPipeline(app => { /* ... */ });
```

### 2.2 Multiple Broker Instances

A service may need to connect to multiple RabbitMQ instances simultaneously (e.g., an internal broker for domain events and an external broker for third-party integrations). Wolverine supports this via named brokers (`BrokerName`). Hive wraps this with a fluent API:

```csharp
var service = new MicroService("gateway-service")
    .WithMessaging(messaging => messaging
        // Primary broker (unnamed — used by default)
        .UseRabbitMq(rabbit => rabbit
            .ConnectionUri("amqp://internal-rabbit")
            .AutoProvision())

        // Named secondary broker
        .UseRabbitMq("external", rabbit => rabbit
            .ConnectionUri("amqp://partner-rabbit")
            .AutoProvision())

        .WithSending(sending => sending
            // Publish to primary broker (default)
            .Publish<OrderPlaced>().ToExchange("orders")
            // Publish to named broker
            .Publish<PartnerNotification>().OnBroker("external").ToExchange("notifications"))

        .WithHandling(handling => handling
            // Listen on primary broker (default)
            .ListenToQueue("orders")
            // Listen on named broker
            .ListenToQueue("partner-events").OnBroker("external")))
    .ConfigureApiPipeline(app => { /* ... */ });
```

#### Multi-Broker Configuration via JSON

```json
{
  "Hive": {
    "Messaging": {
      "Transport": "RabbitMQ",
      "RabbitMq": {
        "ConnectionUri": "amqp://internal-rabbit",
        "AutoProvision": true
      },
      "NamedBrokers": {
        "external": {
          "RabbitMq": {
            "ConnectionUri": "amqp://partner-rabbit",
            "AutoProvision": true
          }
        }
      }
    }
  }
}
```

#### How It Works Internally

Hive maps named brokers to Wolverine's `BrokerName` + `AddNamedRabbitMqBroker()`:

```csharp
// Primary (unnamed)
opts.UseRabbitMq(new Uri("amqp://internal-rabbit")).AutoProvision();

// Named broker — Wolverine creates a separate connection
var external = new BrokerName("external");
opts.AddNamedRabbitMqBroker(external, factory =>
{
    factory.Uri = new Uri("amqp://partner-rabbit");
});

// Endpoints bound to the named broker use its URI scheme
opts.ListenToRabbitQueueOnNamedBroker(external, "partner-events");
opts.PublishMessage<PartnerNotification>()
    .ToRabbitExchangeOnNamedBroker(external, "notifications");
```

Wolverine opens separate connections per named broker. Each broker maintains its own exchanges, queues, and bindings independently.

### 2.3 Azure Functions Support (Send-Only)

`FunctionHost` implements `IMicroServiceCore`, not `IMicroService`. Message handling (`.WithHandling()`) requires `IMicroService` because it depends on the `IsReady` lifecycle gate, Kubernetes readiness probes, and graceful shutdown semantics — none of which exist in the Azure Functions hosting model. Functions consume messages via their own trigger bindings; Wolverine listeners would conflict with the Functions runtime.

`FunctionHost` can use Hive.Messaging for **sending only**:

```csharp
var host = new FunctionHost("order-processor")
    .WithMessaging(messaging => messaging
        .UseRabbitMq("amqp://localhost")
        .WithSending(s => s
            .Publish<OrderPlaced>().ToExchange("orders")));

await host.RunAsync();
```

The `IMicroServiceCore` overload of `WithMessaging()` exposes a restricted builder that provides transport configuration, serialization, and send-side capabilities. `.WithHandling()` is not available on this builder — attempting to consume messages from a `FunctionHost` is a compile-time error.

### 2.4 Message Handlers

Wolverine discovers handlers by convention (classes ending in `Handler`/`Consumer`, methods named `Handle`/`Consume`). Hive provides optional base classes with lifecycle hooks, but **telemetry is applied to all handlers** via Wolverine middleware regardless of whether the base class is used.

#### Handler DI Registration

Wolverine auto-discovers and auto-registers handlers in the DI container — no manual `services.AddTransient<MyHandler>()` is needed. Handlers are resolved **per-message** with a scoped lifetime, meaning each message gets a fresh handler instance with its own scoped dependencies (`DbContext`, `IMessageBus`, etc.). This is Wolverine's default behavior; Hive does not override it.

```csharp
// Constructor injection works as expected — dependencies resolved from the scoped container
public class OrderPlacedHandler : HiveMessageHandler<OrderPlaced>
{
    private readonly AppDbContext _db;
    private readonly ILogger<OrderPlacedHandler> _logger;

    public OrderPlacedHandler(AppDbContext db, ILogger<OrderPlacedHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public override async Task HandleAsync(OrderPlaced message, CancellationToken ct)
    {
        // _db is a fresh scoped instance for this message
    }
}
```

```csharp
// Option A: Pure Wolverine convention — telemetry still captured by middleware
public class OrderPlacedHandler
{
    public async Task Handle(OrderPlaced message, IMessageContext context)
    {
        // Business logic
    }
}

// Option B: Hive base handler — adds OnError/OnSuccess hooks
public class OrderPlacedHandler : HiveMessageHandler<OrderPlaced>
{
    public override async Task HandleAsync(OrderPlaced message, CancellationToken ct)
    {
        // Business logic
    }
}

// Option C: Hive base handler with cascading response
public class PlaceOrderHandler : HiveMessageHandler<PlaceOrder, OrderPlaced>
{
    public override async Task<OrderPlaced> HandleAsync(PlaceOrder message, CancellationToken ct)
    {
        var order = await CreateOrderAsync(message);
        return new OrderPlaced(order.Id); // Wolverine publishes this automatically
    }
}
```

#### HiveMessageHandler Base Classes

```csharp
// Fire-and-forget handler — no cascading message
public abstract class HiveMessageHandler<TMessage>
{
    public abstract Task HandleAsync(TMessage message, CancellationToken ct);

    // Called on every failed attempt (before Wolverine schedules a retry)
    protected virtual Task OnRetryAsync(TMessage message, Exception exception, int attempt, CancellationToken ct)
        => Task.CompletedTask;

    // Called only when all retries are exhausted — final failure
    protected virtual Task OnErrorAsync(TMessage message, Exception exception, CancellationToken ct)
        => Task.CompletedTask;

    protected virtual Task OnSuccessAsync(TMessage message, CancellationToken ct)
        => Task.CompletedTask;
}

// Handler with cascading response — Wolverine publishes TResponse automatically
public abstract class HiveMessageHandler<TMessage, TResponse>
{
    public abstract Task<TResponse> HandleAsync(TMessage message, CancellationToken ct);

    // Called on every failed attempt (before Wolverine schedules a retry)
    protected virtual Task OnRetryAsync(TMessage message, Exception exception, int attempt, CancellationToken ct)
        => Task.CompletedTask;

    // Called only when all retries are exhausted — final failure
    protected virtual Task OnErrorAsync(TMessage message, Exception exception, CancellationToken ct)
        => Task.CompletedTask;

    protected virtual Task OnSuccessAsync(TMessage message, TResponse response, CancellationToken ct)
        => Task.CompletedTask;
}
```

#### Wolverine Middleware Chain

Two Wolverine middlewares are registered in order via `opts.Policies.AddMiddleware<T>()`. They run for **every handler** in the pipeline — pure Wolverine handlers, `HiveMessageHandler<T>`, and `HiveMessageHandler<T, TResponse>` alike.

**1. `ReadinessMiddleware`** — Short-circuit guard

- Checks `IMicroService.IsReady`
- If `false`, increments `hive.messaging.gate.nacked` counter and **throws `ServiceNotReadyException`**
- A Wolverine error policy (`opts.Policies.OnException<ServiceNotReadyException>().PauseThenRequeue(5.Seconds())`) handles the exception by **pausing the entire listener** for 5 seconds, then requeuing the message
- No hooks, no telemetry — single responsibility: check readiness, throw if not ready
- Ensures a service that is shutting down (or not yet ready) never processes new messages; the listener pause prevents further delivery until the readiness state changes

**Why exception + error policy?** Wolverine middlewares cannot directly nack a message — `HandlerContinuation.Stop` causes an ACK (message consumed), not a requeue. The only way to trigger a requeue from middleware is through Wolverine's exception-handling pipeline. The `ServiceNotReadyException` + `PauseThenRequeue` pattern cleanly separates the decision (middleware) from the consequence (error policy), and `.PauseThenRequeue()` is the correct behavior: when `IsReady == false`, *no* messages should be processed on that listener, not just the current one.

```csharp
public sealed class ServiceNotReadyException : Exception
{
    public ServiceNotReadyException(string serviceName)
        : base($"Service '{serviceName}' is not ready to process messages") { }
}

public static class ReadinessMiddleware
{
    public static void Before(IMicroService microService)
    {
        if (!microService.IsReady)
        {
            MessagingMeter.GateNacked.Add(1);
            throw new ServiceNotReadyException(microService.Name);
        }
    }
}
```

**2. `MessageHandlerMiddleware`** — Telemetry and lifecycle hooks

- Starts a telemetry span with message type, queue, and handler tags
- Records `hive.messaging.handler.duration` histogram
- Increments `hive.messaging.handler.count` counter
- Increments `hive.messaging.handler.errors` counter on failure
- Logs at `Debug` level: message received, handler started, handler completed/failed
- Catches exceptions, then re-throws for Wolverine's retry pipeline

For `HiveMessageHandler` subclasses, the middleware additionally invokes lifecycle hooks:
- `OnRetryAsync` — called on every failed attempt before Wolverine schedules a retry (includes the attempt number)
- `OnErrorAsync` — called only when all retries are exhausted (final failure)
- `OnSuccessAsync` — called after successful handling

Pure Wolverine handlers get telemetry without the hooks.

### 2.5 Message Sending

Publishing uses Wolverine's `IMessageBus` directly (scoped service). Send-side telemetry uses a dual-layer approach:

- **DI decorator** — A `TelemetryMessageBus` wraps `IMessageBus` transparently (users still inject `IMessageBus`). Captures `send.count` and `send.errors` for explicit user sends with precise Hive tags.
- **ActivityListener** — Subscribes to Wolverine's `ActivitySource("Wolverine")` to capture implicit sends (cascading messages from handler return values, outbox relay via DurabilityAgent, scheduled/delayed messages). Skips sends already tracked by the decorator to avoid double-counting.

| Send path | Tracked by |
|-----------|-----------|
| `bus.PublishAsync()` / `bus.SendAsync()` | Decorator |
| Cascading return values from handlers | ActivityListener |
| Outbox relay (DurabilityAgent) | ActivityListener |
| Scheduled/delayed messages | ActivityListener |

```csharp
// In an API endpoint or service
app.MapPost("/orders", async (CreateOrder command, IMessageBus bus) =>
{
    var order = ProcessOrder(command);
    await bus.PublishAsync(new OrderPlaced(order.Id));
    return Results.Created($"/orders/{order.Id}", order);
});

// In a message handler (cascading messages via return value)
public class CreateOrderHandler : HiveMessageHandler<CreateOrder, OrderPlaced>
{
    public override async Task<OrderPlaced> HandleAsync(CreateOrder message, CancellationToken ct)
    {
        var order = await CreateOrderAsync(message);
        return new OrderPlaced(order.Id); // Wolverine publishes this automatically
    }
}
```

### 2.6 Serialization

```csharp
messaging.WithSerialization(s => s
    .UseSystemTextJson())           // Default — mandatory

messaging.WithSerialization(s => s
    .UseMessagePack())              // Binary, high performance

messaging.WithSerialization(s => s
    .UseProtobuf())                 // Binary, cross-platform

// Per-endpoint override
messaging.WithHandling(h => h
    .ListenToQueue("legacy-events")
    .UseSerialization(s => s.UseNewtonsoftJson()))
```

Serialization is delegated to Wolverine's serializer infrastructure. Hive's `SerializationBuilder` is a thin wrapper that configures `WolverineOptions` accordingly.

### 2.7 Durability (Inbox/Outbox)

Uses Wolverine's built-in EF Core transactional outbox with Wolverine-managed schema:

```csharp
messaging.WithDurability(d => d
    .UseEntityFrameworkCore<AppDbContext>()
    .UseDurableInbox()       // Persist incoming messages before processing
    .UseDurableOutbox())     // Persist outgoing messages in the same transaction
```

Wolverine manages its own envelope tables. The `[Transactional]` attribute on handlers ensures atomicity:

```csharp
[Transactional]
public class PlaceOrderHandler : HiveMessageHandler<PlaceOrder, OrderPlaced>
{
    private readonly AppDbContext _db;

    public PlaceOrderHandler(AppDbContext db) => _db = db;

    public override async Task<OrderPlaced> HandleAsync(PlaceOrder message, CancellationToken ct)
    {
        var order = new Order(message.ProductId, message.Quantity);
        _db.Orders.Add(order);

        // The returned OrderPlaced is persisted in the outbox within the same DB transaction.
        // Wolverine's DurabilityAgent delivers it to the broker asynchronously.
        return new OrderPlaced(order.Id);
    }
}
```

---

## 3. Configuration

### 3.1 JSON Configuration

```json
{
  "Hive": {
    "Messaging": {
      "Transport": "RabbitMQ",
      "RabbitMq": {
        "ConnectionUri": "amqp://guest:guest@localhost:5672",
        "AutoProvision": false
      },
      "Serialization": "SystemTextJson",
      "Handling": {
        "PrefetchCount": 10,
        "ListenerCount": 1
      }
    }
  }
}
```

The `Handling` section defines global defaults for all listeners. Individual queues can override these via the fluent API (see Section 2.4).

When `Hive.Messaging.EntityFrameworkCore` is added, durability configuration becomes available:

```json
{
  "Hive": {
    "Messaging": {
      "Durability": {
        "Enabled": true,
        "InboxEnabled": true,
        "OutboxEnabled": true
      }
    }
  }
}
```

### 3.2 Tiered Configuration

Same model as Hive.HTTP: `IConfiguration < Fluent API` (fluent wins).

```csharp
// appsettings.Development.json provides the connection URI
// Fluent API adds queue/exchange topology
service.WithMessaging(m => m
    .WithSending(s => s.Publish<OrderPlaced>().ToExchange("orders"))
    .WithHandling(h => h.ListenToQueue("orders")));
```

### 3.3 Configuration Options

```csharp
public enum MessagingTransport
{
    RabbitMQ,
    InMemory
}

public enum MessagingSerialization
{
    SystemTextJson,
    NewtonsoftJson,
    MessagePack,
    Protobuf
}

public class MessagingOptions
{
    public const string SectionKey = "Hive:Messaging";
    public MessagingTransport Transport { get; set; } = MessagingTransport.RabbitMQ;
    public RabbitMqOptions RabbitMq { get; set; } = new();
    public MessagingSerialization Serialization { get; set; } = MessagingSerialization.SystemTextJson;
    public Dictionary<string, NamedBrokerOptions> NamedBrokers { get; set; } = new();
    public HandlingOptions Handling { get; set; } = new();
}

public class HandlingOptions
{
    public int? PrefetchCount { get; set; }     // null = Wolverine default
    public int? ListenerCount { get; set; }     // null = Wolverine default
}

public class NamedBrokerOptions
{
    public RabbitMqOptions RabbitMq { get; set; } = new();
}

public class RabbitMqOptions
{
    public string? ConnectionUri { get; set; }
    public bool AutoProvision { get; set; }
}

// In Hive.Messaging.EntityFrameworkCore assembly
public class DurabilityOptions
{
    public bool Enabled { get; set; }
    public bool InboxEnabled { get; set; }
    public bool OutboxEnabled { get; set; }
}
```

---

## 4. Telemetry Design

### 4.1 Comparison: Wrap vs Replace Wolverine Telemetry

| Aspect | Wrap (Recommended) | Replace |
|--------|-------------------|---------|
| **Wolverine's built-in spans** | Kept — retries, DLQ, circuit breaker events visible | Lost — must reimplement |
| **Hive-specific tags** | Added via middleware (`service.name`, `handler.type`) | Full control over all tags |
| **Metric naming** | Hive meter (`hive.messaging.*`) alongside Wolverine's | Only Hive meter |
| **Maintenance** | Low — Wolverine maintains its internals | High — must track Wolverine changes |
| **Consistency with Hive.HTTP** | Hive meter follows same naming pattern | Identical pattern |
| **Observability depth** | Deep — Wolverine internals + Hive business metrics | Shallow unless we hook deeply |

**Recommendation: Wrap.** Keep Wolverine's `ActivitySource("Wolverine")` for detailed transport/infrastructure traces. Add a `Hive.Messaging` meter for business-level metrics consistent with the Hive.HTTP pattern.

### 4.2 Hive.Messaging Meter

```csharp
internal static class MessagingMeter
{
    private static readonly Meter Meter = new("Hive.Messaging");

    public static readonly Histogram<double> HandlerDuration =
        Meter.CreateHistogram<double>(
            "hive.messaging.handler.duration",
            unit: "ms",
            description: "Message handler execution duration in milliseconds");

    public static readonly Counter<long> HandlerCount =
        Meter.CreateCounter<long>(
            "hive.messaging.handler.count",
            description: "Total messages handled");

    public static readonly Counter<long> HandlerErrors =
        Meter.CreateCounter<long>(
            "hive.messaging.handler.errors",
            description: "Failed message handler executions");

    public static readonly Counter<long> MessagesSent =
        Meter.CreateCounter<long>(
            "hive.messaging.send.count",
            description: "Total messages sent/published");

    public static readonly Counter<long> SendErrors =
        Meter.CreateCounter<long>(
            "hive.messaging.send.errors",
            description: "Failed message send attempts");

    public static readonly Counter<long> GateNacked =
        Meter.CreateCounter<long>(
            "hive.messaging.gate.nacked",
            description: "Messages nacked by ReadinessMiddleware (IsReady == false)");
}
```

### 4.3 Tag Dimensions

| Tag | Description | Applied To |
|-----|-------------|------------|
| `service.name` | The Hive microservice name | All metrics |
| `messaging.message.type` | Message CLR type name | All metrics |
| `messaging.destination` | Queue/exchange name | Send metrics |
| `messaging.source` | Source queue name | Handler metrics |
| `handler.type` | Handler CLR type name | Handler metrics |
| `error.type` | Exception type name | Error metrics only |

### 4.4 OpenTelemetry Integration

```csharp
// In the consuming service's OpenTelemetry setup
service.WithOpenTelemetry(
    tracing: t => t.AddSource("Wolverine"),      // Wolverine's built-in traces
    metrics: m => m.AddMeter("Hive.Messaging")   // Hive's business metrics
);
```

Both `Wolverine`'s ActivitySource and `Hive.Messaging`'s Meter are registered. The user gets transport-level detail from Wolverine and business-level metrics from Hive.

---

## 5. Handler Pipeline

```mermaid
sequenceDiagram
    participant Broker as RabbitMQ
    participant Wolverine as Wolverine Runtime
    participant Gate as ReadinessMiddleware
    participant Service as IMicroService
    participant Middleware as MessageHandlerMiddleware
    participant Handler as HiveMessageHandler<T>
    participant Outbox as Outbox (EF Core)
    participant Telemetry as Hive.Messaging Meter

    Broker->>Wolverine: Deliver message
    Wolverine->>Wolverine: Deserialize (configured serializer)
    Wolverine->>Gate: Invoke handler chain
    Gate->>Service: Check IsReady

    alt IsReady == false
        Gate->>Telemetry: Increment gate.nacked
        Gate->>Wolverine: throw ServiceNotReadyException
        Note over Wolverine: Error policy: PauseThenRequeue(5s)
        Wolverine->>Wolverine: Pause listener for 5s
        Wolverine-->>Broker: Requeue message
    else IsReady == true
        Gate->>Middleware: Pass to next middleware
        Middleware->>Telemetry: Start span + timer
        Middleware->>Handler: HandleAsync(message, ct)

        alt Success
            Handler-->>Middleware: Completed
            Middleware->>Telemetry: Record duration, increment count
            Handler->>Outbox: Cascading messages (if [Transactional])
            Wolverine-->>Broker: Ack
        else Failure (attempt < max retries)
            Handler-->>Middleware: Exception
            Middleware->>Telemetry: Record duration, increment errors
            Middleware->>Handler: OnRetryAsync(message, ex, attempt, ct)
            Middleware-->>Wolverine: Re-throw
            Note over Wolverine,Broker: Wolverine schedules retry per policy (backoff/cooldown)
            Note over Wolverine,Gate: Retry re-enters the full pipeline (Gate → Middleware → Handler)
        else Failure (all retries exhausted)
            Handler-->>Middleware: Exception (final)
            Middleware->>Telemetry: Record duration, increment errors
            Middleware->>Handler: OnErrorAsync(message, ex, ct)
            Middleware-->>Wolverine: Re-throw
            Wolverine-->>Broker: Nack / move to error queue
        end
    end
```

---

## 6. Sending Pipeline

### Explicit sends (via IMessageBus)

```mermaid
sequenceDiagram
    participant App as Application Code
    participant Decorator as TelemetryMessageBus
    participant Bus as IMessageBus (Wolverine)
    participant Telemetry as Hive.Messaging Meter
    participant Outbox as Outbox (if durable)
    participant Broker as RabbitMQ

    App->>Decorator: PublishAsync(message)
    Decorator->>Telemetry: Increment send.count
    Decorator->>Decorator: Mark Activity (hive.messaging.tracked)
    Decorator->>Bus: PublishAsync(message)
    Bus->>Bus: Serialize (configured serializer)

    alt Durable Outbox
        Bus->>Outbox: Persist envelope in DB transaction
        Outbox-->>Bus: Persisted
        Note over Outbox,Broker: DurabilityAgent delivers async
        Outbox->>Broker: Deliver envelope
    else Direct Send
        Bus->>Broker: Publish directly
    end

    Broker-->>Bus: Ack
```

### Implicit sends (cascading, outbox relay)

```mermaid
sequenceDiagram
    participant Wolverine as Wolverine Runtime
    participant Listener as WolverineSendActivityListener
    participant Telemetry as Hive.Messaging Meter
    participant Broker as RabbitMQ

    Wolverine->>Wolverine: Send cascading/outbox message
    Wolverine->>Listener: Activity stopped (Wolverine ActivitySource)
    Listener->>Listener: Check hive.messaging.tracked tag
    alt Not already tracked
        Listener->>Telemetry: Increment send.count
    else Already tracked by decorator
        Listener->>Listener: Skip (no double-count)
    end
    Wolverine->>Broker: Deliver message
```

---

## 7. Graceful Shutdown

`IMicroService.IsReady` is the authoritative signal for whether a service should process messages. It is directly linked to the Kubernetes readiness probe (`/readiness`). When `IsReady == false`, the service **must not process any new messages** — they must be nacked and returned to the queue for redelivery to a healthy instance.

This applies in two scenarios:

1. **Service not yet ready** — During startup, `IsReady` is `false` until the service completes initialization. Any messages delivered before readiness are nacked.
2. **Service shutting down** — On SIGTERM, `IsReady` is set to `false` immediately. New messages arriving during drain are nacked; in-flight messages are allowed to complete.

### Shutdown Sequence

When a service receives a shutdown signal (e.g., SIGTERM from Kubernetes):

1. **`IsReady = false`** — The readiness probe fails; Kubernetes stops routing HTTP traffic. The `ReadinessMiddleware` throws `ServiceNotReadyException` on any newly delivered message. Wolverine's `PauseThenRequeue` error policy pauses each listener, preventing further delivery during the window before the drain completes.
2. **Drain listeners** — Wolverine's listeners are drained (`DrainAsync()`); no further messages are dequeued from the broker.
3. **Complete in-flight messages** — Handlers currently executing are allowed to finish within the shutdown timeout. These handlers were accepted while `IsReady` was still `true`.
4. **Flush the outbox** — Any pending outbox messages are delivered before the process exits.

### Integration with Hive Lifecycle

```mermaid
sequenceDiagram
    participant K8s as Kubernetes
    participant Hive as Hive StopAsync()
    participant Gate as ReadinessMiddleware
    participant Wolverine as Wolverine Runtime
    participant Handlers as In-Flight Handlers
    participant Broker as RabbitMQ

    K8s->>Hive: SIGTERM
    Hive->>Hive: IsReady = false

    Note over Gate: New messages trigger ServiceNotReadyException
    Note over Gate: PauseThenRequeue pauses listeners

    Hive->>Wolverine: DrainAsync()
    Wolverine->>Broker: Stop consuming (prefetch = 0)
    Wolverine->>Handlers: Await completion (with CancellationToken)

    alt Handlers complete within timeout
        Handlers-->>Wolverine: Done
        Wolverine->>Wolverine: Flush outbox
        Wolverine->>Broker: Ack remaining messages
    else Timeout exceeded
        Wolverine->>Broker: Nack unfinished messages (requeue)
    end

    Wolverine-->>Hive: Shutdown complete
    Hive->>K8s: Process exits
```

Wolverine's built-in `IDurabilityAgent` handles the drain and outbox flush. Hive integrates this into its `StopAsync()` lifecycle by calling `DrainAsync()` on the Wolverine runtime. The `IsReady = false` assignment happens **before** the drain, creating a two-layer defense:

1. **Middleware layer** — `ReadinessMiddleware` throws `ServiceNotReadyException` when `IsReady == false`. The `PauseThenRequeue` error policy pauses listeners and requeues the message. This covers the window between `IsReady = false` and `DrainAsync()` completing.
2. **Transport layer** — `DrainAsync()` stops the broker from delivering further messages entirely.

Handlers should respect the `CancellationToken` passed to `HandleAsync`. When the shutdown timeout is exceeded, the token is cancelled. Messages whose handlers did not complete are nacked and requeued by the broker for redelivery to another instance.

---

## 8. Health Checks

The extension registers health checks via the Hive `ConfigureHealthChecks(IHealthChecksBuilder)` lifecycle method:

- **Broker connectivity** — Verifies the RabbitMQ connection is alive. Fails the readiness probe if the broker is unreachable.
- **Named broker connectivity** — Each named broker gets its own health check entry.

```csharp
// Registered automatically by the extension. No user code needed.
// Health check names follow the pattern: "rabbitmq" (primary), "rabbitmq:external" (named)
```

Health checks affect the `/readiness` K8s probe. A service that cannot reach its broker should not receive traffic.

---

## 9. Serialization Strategy

### Current State

`Hive.Serialization` exists as a static utility class in `Hive.Abstractions/Serialization.cs` providing `JsonSerializerOptions`. It is **not** a separate assembly.

### Proposed Approach

Keep serialization configuration within `Hive.Messaging` using Wolverine's serializer infrastructure. No need for a separate `Hive.Serialization` assembly — Wolverine already provides the serializer abstraction (`IMessageSerializer`) and ships packages for JSON, MessagePack, and Protobuf.

| Serializer | Package | MIME Type | Status |
|------------|---------|-----------|--------|
| System.Text.Json | Built into WolverineFx | `application/json` | Mandatory (default) |
| Newtonsoft.Json | Built into WolverineFx | `application/json` | Available |
| MessagePack | `WolverineFx.MessagePack` | `application/vnd.msgpack` | Nice to have |
| Protobuf | `WolverineFx.Protobuf` | `application/x-protobuf` | Nice to have |

If a shared serialization abstraction becomes needed across multiple Hive extensions in the future, it can be extracted into `Hive.Serialization` as a namespace under `hive.extensions/src/` at that point.

---

## 10. Testing Support (Hive.Messaging.Testing)

Following the Hive.HTTP.Testing pattern:

```csharp
// In-memory transport for integration tests (Wolverine's built-in)
var service = new MicroService("test-service")
    .WithMessaging(m => m
        .UseInMemoryTransport()   // No real broker needed
        .WithHandling(h => h.ListenToQueue("test-queue")))
    .WithTestTracking();          // Wolverine's test session tracking

// Assert messages were sent
var session = await service.ExecuteAndWaitAsync(async bus =>
{
    await bus.PublishAsync(new OrderPlaced(42));
});

session.Sent.SingleMessage<OrderPlaced>()
    .OrderId.Should().Be(42);
```

---

## 11. Client Library Pattern

A service can publish a client NuGet package containing its message contracts and a configuration fragment. Unlike Hive.HTTP's client libraries (which register independent `HttpClient` instances), messaging uses a single Wolverine host per process — so client libraries extend the builder rather than calling `.WithMessaging()` themselves.

```
OrderService.Messaging.Client/
├── Messages/
│   ├── OrderPlaced.cs
│   └── OrderCancelled.cs
└── MessagingBuilderExtensions.cs
```

```csharp
// Client library — extends HiveMessagingBuilder, not IMicroService
public static class MessagingBuilderExtensions
{
    public static HiveMessagingBuilder WithOrderServiceMessages(this HiveMessagingBuilder builder)
    {
        builder.WithHandling(h => h.ListenToExchange("order-events"));
        return builder;
    }
}

// Consumer — composes client fragment inside their single .WithMessaging() call
var service = new MicroService("notification-service")
    .WithMessaging(m => m
        .UseRabbitMq("amqp://localhost")
        .WithOrderServiceMessages()
        .WithHandling(h => h.ListenToQueue("my-own-queue")))
    .ConfigureApiPipeline(app => { });
```

This ensures `AddWolverine()` is called exactly once per process. Client libraries provide message DTOs and builder configuration; the consuming service owns the single `.WithMessaging()` registration.

---

## 12. Extension Internal Architecture

### Extension Classes

The extension uses a polymorphic two-class design instead of a single class with a boolean flag. This eliminates the `Action<T>` contravariance issue (an `Action<HiveMessagingBuilder>` cannot be stored as `Action<HiveMessagingSendBuilder>`) and follows Replace Conditional with Polymorphism — the type system encodes whether handling is enabled, not a runtime flag.

#### MessagingExtensionBase.cs — Shared logic

```csharp
internal abstract class MessagingExtensionBase<TSelf> : MicroServiceExtension<TSelf>
    where TSelf : MessagingExtensionBase<TSelf>
{
    protected MessagingExtensionBase(IMicroServiceCore service) : base(service) { }

    protected static void ApplyConfiguration(WolverineOptions opts, MessagingOptions options)
    {
        // Transport
        switch (options.Transport)
        {
            case MessagingTransport.RabbitMQ:
                var rabbit = opts.UseRabbitMq(new Uri(options.RabbitMq.ConnectionUri!));
                if (options.RabbitMq.AutoProvision) rabbit.AutoProvision();
                break;
            case MessagingTransport.InMemory:
                opts.UseInMemoryTransport();
                break;
        }

        // Named brokers
        foreach (var (name, namedOptions) in options.NamedBrokers)
        {
            var broker = new BrokerName(name);
            opts.AddNamedRabbitMqBroker(broker, f => f.Uri = new Uri(namedOptions.RabbitMq.ConnectionUri!));
        }

        // Handling defaults — applied to all listeners; per-queue fluent overrides win
        opts.Policies.ConfigureListeners(listener =>
        {
            if (options.Handling.PrefetchCount.HasValue)
                listener.PreFetchCount(options.Handling.PrefetchCount.Value);
            if (options.Handling.ListenerCount.HasValue)
                listener.ListenerCount(options.Handling.ListenerCount.Value);
        });
    }

    protected void ConfigureWolverineCore(
        IServiceCollection svc,
        IConfiguration configuration,
        IMicroServiceCore microservice,
        Action<WolverineOptions, MessagingOptions> applyBuilder)
    {
        var messagingSection = configuration.GetSection(MessagingOptions.SectionKey);
        var options = new MessagingOptions();
        if (messagingSection.Exists())
            messagingSection.Bind(options);

        svc.AddWolverine(opts =>
        {
            opts.ServiceName = microservice.Name;

            // Apply IConfiguration defaults (transport, serialization, handling defaults)
            ApplyConfiguration(opts, options);

            // Delegate to subclass for builder creation, fluent config, and middleware
            applyBuilder(opts, options);
        });

        // Send-side telemetry — registered for both IMicroService and IMicroServiceCore
        svc.Decorate<IMessageBus, TelemetryMessageBus>();
        svc.AddSingleton<WolverineSendActivityListener>();
        svc.AddHostedService(sp => sp.GetRequiredService<WolverineSendActivityListener>());
    }
}
```

#### MessagingExtension.cs — Full builder (handlers + send + readiness gate)

```csharp
internal sealed class MessagingExtension : MessagingExtensionBase<MessagingExtension>
{
    private readonly Action<HiveMessagingBuilder> _configure;

    public MessagingExtension(IMicroServiceCore service, Action<HiveMessagingBuilder> configure)
        : base(service) => _configure = configure;

    public override IServiceCollection ConfigureServices(
        IServiceCollection services,
        IMicroServiceCore microservice)
    {
        ConfigureActions.Add((svc, configuration) =>
        {
            ConfigureWolverineCore(svc, configuration, microservice, (opts, options) =>
            {
                var builder = new HiveMessagingBuilder(opts, options);
                _configure(builder);

                // Register Wolverine middlewares (order matters — gate runs first)
                opts.Policies.AddMiddleware<ReadinessMiddleware>();
                opts.Policies.AddMiddleware<MessageHandlerMiddleware>();

                // Readiness gate error policy — pause listener and requeue
                opts.Policies.OnException<ServiceNotReadyException>()
                    .PauseThenRequeue(5.Seconds());

                var result = Validator.Validate(builder);
                if (!result.IsValid)
                    throw new OptionsValidationException(
                        MessagingOptions.SectionKey, typeof(MessagingOptions), result.Errors);
            });
        });

        return services;
    }
}
```

#### MessagingSendExtension.cs — Send-only builder (no handlers, no readiness gate)

```csharp
internal sealed class MessagingSendExtension : MessagingExtensionBase<MessagingSendExtension>
{
    private readonly Action<HiveMessagingSendBuilder> _configure;

    public MessagingSendExtension(IMicroServiceCore service, Action<HiveMessagingSendBuilder> configure)
        : base(service) => _configure = configure;

    public override IServiceCollection ConfigureServices(
        IServiceCollection services,
        IMicroServiceCore microservice)
    {
        ConfigureActions.Add((svc, configuration) =>
        {
            ConfigureWolverineCore(svc, configuration, microservice, (opts, options) =>
            {
                var builder = new HiveMessagingSendBuilder(opts, options);
                _configure(builder);

                var result = Validator.Validate(builder);
                if (!result.IsValid)
                    throw new OptionsValidationException(
                        MessagingOptions.SectionKey, typeof(MessagingOptions), result.Errors);
            });
        });

        return services;
    }
}
```

### Startup.cs

```csharp
public static class Startup
{
    // Full builder — send + receive + handling + durability
    public static IMicroService WithMessaging(
        this IMicroService service,
        Action<HiveMessagingBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(configure);

        var extension = new MessagingExtension(service, configure);
        service.Extensions.Add(extension);
        return service;
    }

    // Restricted builder — send-only (no WithHandling, no readiness gate)
    public static IMicroServiceCore WithMessaging(
        this IMicroServiceCore service,
        Action<HiveMessagingSendBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(configure);

        var extension = new MessagingSendExtension(service, configure);
        service.Extensions.Add(extension);
        return service;
    }
}
```

`HiveMessagingBuilder` extends `HiveMessagingSendBuilder`, adding `.WithHandling()` and `.WithDurability()`. The `IMicroServiceCore` overload creates a `MessagingSendExtension` (no middlewares, no readiness gate), enforcing at compile time that `FunctionHost` cannot register handlers. Each extension class stores its own correctly-typed `Action<T>` — no variance issues, no casts.

---

## 13. Design Decisions

| # | Question | Decision | Rationale |
|---|----------|----------|-----------|
| 1 | Handler discovery scope | **Auto-discover + opt-out** | Wolverine scans all assemblies by default. Hive allows excluding assemblies via `.WithHandling(h => h.ExcludeAssemblyOf<T>())`. Convenient for most cases, escape hatch for edge cases. |
| 2 | Dead letter queue strategy | **Out of scope** | DLQ routing is configured at the broker level by a dedicated bus management service. Hive.Messaging relies on broker-level dead-letter policies; it does not create or manage DLQ bindings. |
| 3 | Multi-tenancy | **Not in scope** | Skip for initial design. Users can set `TenantId` directly on Wolverine's `DeliveryOptions` if needed. |
| 4 | Message versioning | **Use Wolverine's directly** | Let users use `[MessageIdentity]` and `IForwardsTo<T>` from Wolverine. No Hive wrapper. Document in README. |
| 5 | Retry policy configuration | **Hive builder + escape hatch** | Hive builder for common cases (max retries, backoff). `ConfigureWolverine()` escape hatch for advanced settings. |
| 6 | Telemetry middleware scope | **All handlers** | Wolverine middleware instruments every handler regardless of base class. Users get telemetry whether they use `HiveMessageHandler<T>` or pure Wolverine conventions. |
| 7 | Cascading messages | **Dual base class** | `HiveMessageHandler<TMessage>` for fire-and-forget, `HiveMessageHandler<TMessage, TResponse>` for cascading. Supports Wolverine's return-value pattern without losing Hive hooks. |
| 8 | Send-side abstraction | **IMessageBus with DI decorator + ActivityListener** | Users inject `IMessageBus`. A transparent DI decorator (`TelemetryMessageBus`) captures explicit sends. A `WolverineSendActivityListener` captures implicit sends (cascading, outbox relay). Deduplication via Activity tag. |
| 9 | Transport type | **Enum** | `MessagingTransport` enum (RabbitMQ, InMemory). Compile-time safety, prevents typos. New transports added as enum values. |
| 10 | EF Core durability packaging | **Separate package** | `Hive.Messaging.EntityFrameworkCore` — users who don't need durability don't pull in EF Core. Follows the ecosystem convention (MassTransit, Wolverine). |
| 11 | Message handling host restriction | **`IMicroService` only** | `.WithHandling()` requires `IMicroService` because it depends on `IsReady` lifecycle gating, Kubernetes readiness probes, and graceful shutdown (`DrainAsync`). `FunctionHost` (`IMicroServiceCore`) gets a restricted send-only builder (`HiveMessagingSendBuilder`). Enforced at compile time via separate overloads and builder types. |
| 12 | Readiness gate nack mechanism | **`ServiceNotReadyException` + `PauseThenRequeue`** | Wolverine middlewares cannot directly nack a message — `HandlerContinuation.Stop` triggers an ACK, not a requeue. The middleware throws `ServiceNotReadyException`; Wolverine's error policy `OnException<ServiceNotReadyException>().PauseThenRequeue(5.Seconds())` pauses the listener and requeues the message. This cleanly separates the decision (middleware) from the consequence (error policy), and pausing the entire listener is the correct behavior when `IsReady == false`. |
| 13 | Consumer concurrency configuration | **Global defaults + per-queue override + escape hatch** | `HandlingOptions` in JSON (`Hive:Messaging:Handling`) sets `PrefetchCount` and `ListenerCount` defaults for all listeners. Per-queue fluent overrides (`.Prefetch()`, `.ListenerCount()`, `.Sequential()`) win over global defaults. For advanced parallelism controls (circuit breakers, max parallel messages, batch processing), use `ConfigureWolverine()` — see Section 13.1. Follows the tiered configuration model (`IConfiguration < Fluent API`) established across the framework. `null` values defer to Wolverine's built-in defaults. |

### 13.1 Advanced Configuration via `ConfigureWolverine()` Escape Hatch

Hive.Messaging exposes `PrefetchCount`, `ListenerCount`, and `.Sequential()` as first-class configuration. For advanced parallelism, concurrency, and error-handling controls, use the `ConfigureWolverine()` escape hatch, which provides direct access to `WolverineOptions`.

This is a deliberate design choice: Hive wraps the most common settings to keep the API simple, while `ConfigureWolverine()` gives full access to Wolverine's capabilities without Hive needing to mirror every option.

#### Parallelism & Concurrency

```csharp
service.WithMessaging(messaging => messaging
    .UseRabbitMq("amqp://localhost")
    .WithHandling(h => h
        .ListenToQueue("orders"))
    .ConfigureWolverine(opts =>
    {
        // Limit the maximum number of messages processed in parallel per endpoint
        opts.ListenToRabbitQueue("orders").MaximumParallelMessages(5);

        // Configure buffering for bursty workloads
        opts.ListenToRabbitQueue("orders").BufferedInMemory();
    }));
```

#### Circuit Breaker

```csharp
service.WithMessaging(messaging => messaging
    .UseRabbitMq("amqp://localhost")
    .WithHandling(h => h.ListenToQueue("orders"))
    .ConfigureWolverine(opts =>
    {
        // Pause the listener after repeated failures
        opts.ListenToRabbitQueue("orders").CircuitBreaker(cb =>
        {
            cb.MinimumThreshold = 10;
            cb.FailurePercentageThreshold = 20;
            cb.PauseTime = 30.Seconds();
            cb.TrackingPeriod = 1.Minutes();
        });
    }));
```

#### Error Policies & Retry Strategies

```csharp
service.WithMessaging(messaging => messaging
    .UseRabbitMq("amqp://localhost")
    .WithHandling(h => h.ListenToQueue("orders"))
    .ConfigureWolverine(opts =>
    {
        // Retry with escalating cooldown
        opts.Policies.OnException<TimeoutException>()
            .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds());

        // Move poison messages to the error queue
        opts.Policies.OnException<InvalidOperationException>()
            .MoveToErrorQueue();

        // Requeue after a pause (useful for transient downstream failures)
        opts.Policies.OnException<HttpRequestException>()
            .PauseThenRequeue(5.Seconds());
    }));
```

#### Execution Timeout

```csharp
service.WithMessaging(messaging => messaging
    .UseRabbitMq("amqp://localhost")
    .WithHandling(h => h.ListenToQueue("orders"))
    .ConfigureWolverine(opts =>
    {
        // Cancel handler execution after 30 seconds
        opts.DefaultExecutionTimeout = 30.Seconds();
    }));
```

> **Note:** `ConfigureWolverine()` actions run after Hive's built-in configuration. When configuring specific endpoints (e.g., `opts.ListenToRabbitQueue("orders")`), ensure the queue name matches the one registered via `.WithHandling()`.

### Retry Builder API

```csharp
// Hive builder for common retry patterns
messaging.WithHandling(h => h
    .ListenToQueue("orders")
    .WithRetry(r => r
        .MaxAttempts(3)
        .ExponentialBackoff(baseDelay: TimeSpan.FromSeconds(1))));

// Escape hatch for advanced Wolverine-specific configuration
messaging.ConfigureWolverine(opts =>
{
    opts.Policies.OnException<TimeoutException>()
        .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds());

    opts.Policies.OnException<DatabaseException>()
        .MoveToErrorQueue();
});
```

### Queue Topology

Queue and exchange management (creation, DLQ bindings, TTL policies, flushing) is **not** the responsibility of individual services. A dedicated bus management service owns all broker topology. Hive.Messaging assumes queues and exchanges already exist at runtime.

`AutoProvision` is available as an opt-in convenience (e.g., for local development) but defaults to `false`. Whether it works in practice depends on the credentials and permissions granted to the service by the broker.

---

## 14. Test Plan

All tests use the in-memory transport (`UseInMemoryTransport()`) and Wolverine's test tracking (`WithTestTracking()`) unless stated otherwise. Tests requiring a real broker are marked as `[IntegrationTest]`.

**Readiness and shutdown tests (14.1, 14.2):** These test the middleware's *decision logic* (nack vs pass-through) by asserting handler invocation counts and metric increments — not the actual broker requeue behavior. `ReadinessMiddleware` is a standalone class that can be unit tested directly with a mock `IMicroService`. The broker-level nack/requeue plumbing is Wolverine's responsibility and is covered by the end-to-end test 14.7 #5 (full shutdown under load) as an `[IntegrationTest]`.

### 14.1 Readiness Gate (ReadinessMiddleware)

These tests verify that `IMicroService.IsReady` is the authoritative signal for message processing.

| # | Test | Category | Description |
|---|------|----------|-------------|
| 1 | Messages nacked when IsReady is false at startup | `[UnitTest]` | Start service, publish message **before** `IsReady = true`. Assert message is not handled and is requeued. |
| 2 | Messages processed when IsReady is true | `[UnitTest]` | Start service, wait for `IsReady = true`, publish message. Assert handler is invoked. |
| 3 | Messages nacked after IsReady transitions to false | `[UnitTest]` | Start service, wait for ready, set `IsReady = false`, publish message. Assert message is nacked, handler is **not** invoked. |
| 4 | In-flight handler completes after IsReady transitions to false | `[UnitTest]` | Start service, begin processing a slow message (handler blocks on semaphore), set `IsReady = false`, release semaphore. Assert handler runs to completion and acks. |
| 5 | Nacked messages tracked by gate.nacked counter | `[UnitTest]` | Publish message while `IsReady == false`. Assert `hive.messaging.gate.nacked` is incremented and `hive.messaging.handler.count` is **not** incremented. |
| 6 | Rapid IsReady toggling does not lose messages | `[UnitTest]` | Toggle `IsReady` rapidly while publishing a stream of messages. Assert every message is either handled exactly once or nacked for requeue — none are dropped. |

### 14.2 Graceful Shutdown

| # | Test | Category | Description |
|---|------|----------|-------------|
| 1 | Shutdown sets IsReady to false before drain | `[UnitTest]` | Trigger `StopAsync()`. Assert `IsReady` becomes `false` **before** `DrainAsync()` is called (verify ordering via mocks/callbacks). |
| 2 | New messages nacked during drain window | `[UnitTest]` | Start processing a slow message (holds drain open), publish a second message during the drain. Assert the second message is nacked, the first completes. |
| 3 | In-flight handlers complete within timeout | `[UnitTest]` | Start a handler that takes 500ms. Trigger shutdown with 2s timeout. Assert handler completes successfully and message is acked. |
| 4 | Handlers cancelled after shutdown timeout | `[UnitTest]` | Start a handler that blocks indefinitely. Trigger shutdown with short timeout (100ms). Assert `CancellationToken` is cancelled, message is nacked and requeued. |
| 5 | Outbox flushed before process exits | `[IntegrationTest]` | With durable outbox enabled, handler writes to outbox during shutdown. Assert outbox messages are delivered to the broker before shutdown completes. |
| 6 | Multiple in-flight handlers all complete | `[UnitTest]` | Start 5 concurrent handlers with varying durations. Trigger shutdown. Assert all complete within timeout, all messages acked. |
| 7 | StopAsync is idempotent | `[UnitTest]` | Call `StopAsync()` twice. Assert no exception, drain happens only once. |

### 14.3 Handler Lifecycle Hooks

| # | Test | Category | Description |
|---|------|----------|-------------|
| 1 | OnSuccessAsync called on successful handling | `[UnitTest]` | Handle a message successfully. Assert `OnSuccessAsync` is called with the message. |
| 2 | OnSuccessAsync receives response for cascading handlers | `[UnitTest]` | Use `HiveMessageHandler<T, TResponse>`. Assert `OnSuccessAsync` receives both message and response. |
| 3 | OnRetryAsync called on each failed attempt | `[UnitTest]` | Configure max 3 retries. Handler throws on first 2 attempts, succeeds on 3rd. Assert `OnRetryAsync` called twice with attempt numbers 1 and 2. |
| 4 | OnErrorAsync called only after all retries exhausted | `[UnitTest]` | Configure max 2 retries. Handler always throws. Assert `OnRetryAsync` called twice, then `OnErrorAsync` called once. |
| 5 | OnErrorAsync not called when retries succeed | `[UnitTest]` | Configure max 3 retries. Handler throws once, then succeeds. Assert `OnRetryAsync` called once, `OnErrorAsync` **never** called. |
| 6 | Pure Wolverine handlers get telemetry but no hooks | `[UnitTest]` | Use a plain `Handle()` method (no base class). Assert telemetry is recorded, no hook methods invoked. |
| 7 | Exception in OnRetryAsync does not break retry pipeline | `[UnitTest]` | `OnRetryAsync` throws. Assert the original exception still propagates to Wolverine's retry pipeline, handler is retried. |
| 8 | Exception in OnErrorAsync does not suppress original error | `[UnitTest]` | `OnErrorAsync` throws. Assert the original exception is still reported to Wolverine. |

### 14.4 Telemetry

| # | Test | Category | Description |
|---|------|----------|-------------|
| 1 | Handler duration recorded on success | `[UnitTest]` | Handle a message. Assert `hive.messaging.handler.duration` histogram has one entry. |
| 2 | Handler count incremented on success | `[UnitTest]` | Handle a message. Assert `hive.messaging.handler.count` incremented by 1. |
| 3 | Handler errors incremented on failure | `[UnitTest]` | Handler throws. Assert `hive.messaging.handler.errors` incremented by 1. |
| 4 | Send count incremented on explicit publish | `[UnitTest]` | Call `bus.PublishAsync()`. Assert `hive.messaging.send.count` incremented by 1. |
| 5 | Send errors incremented on publish failure | `[UnitTest]` | Publish to unreachable broker. Assert `hive.messaging.send.errors` incremented. |
| 6 | Cascading sends tracked by ActivityListener | `[UnitTest]` | Use `HiveMessageHandler<T, TResponse>`. Assert `send.count` incremented for the cascading message via listener, not decorator. |
| 7 | No double-counting for explicit sends | `[UnitTest]` | Call `bus.PublishAsync()`. Assert `send.count` incremented exactly once (decorator tracks, listener skips). |
| 8 | Tags include service name, message type, handler type | `[UnitTest]` | Handle a message. Assert all tag dimensions are present on the recorded metric. |

### 14.5 Configuration & Registration

| # | Test | Category | Description |
|---|------|----------|-------------|
| 1 | Service starts with minimal configuration | `[UnitTest]` | Register `.WithMessaging(m => m.UseInMemoryTransport())`. Assert service starts successfully. |
| 2 | Service fails to start with missing ConnectionUri | `[UnitTest]` | Register `.WithMessaging(m => m.UseRabbitMq())` with no URI in config. Assert `OptionsValidationException` during startup. |
| 3 | JSON config bound to MessagingOptions | `[UnitTest]` | Provide JSON config with Transport, Serialization, RabbitMq section. Assert options are correctly bound. |
| 4 | Fluent API overrides JSON config | `[UnitTest]` | Provide URI in JSON, override with different URI in fluent. Assert fluent URI wins. |
| 5 | Named broker registered alongside primary | `[UnitTest]` | Register primary + named broker. Assert both connections are available. |
| 6 | Invalid transport enum value rejected | `[UnitTest]` | Set `Transport` to an invalid value in JSON. Assert validation failure. |
| 7 | WithMessaging called twice throws | `[UnitTest]` | Call `.WithMessaging()` twice on the same service. Assert exception (single Wolverine host per process). |

### 14.6 Health Checks

| # | Test | Category | Description |
|---|------|----------|-------------|
| 1 | Broker health check registered automatically | `[UnitTest]` | Register messaging with RabbitMQ. Assert health check named `rabbitmq` exists. |
| 2 | Named broker health check registered | `[UnitTest]` | Register named broker `external`. Assert health check named `rabbitmq:external` exists. |
| 3 | Health check fails when broker unreachable | `[IntegrationTest]` | Point to unreachable broker URI. Assert health check returns `Unhealthy`. |
| 4 | Health check succeeds when broker reachable | `[IntegrationTest]` | Point to running RabbitMQ instance. Assert health check returns `Healthy`. |

### 14.7 End-to-End (Integration)

| # | Test | Category | Description |
|---|------|----------|-------------|
| 1 | Publish and consume round-trip | `[IntegrationTest]` | Publish message via `IMessageBus`, assert handler receives it. In-memory transport. |
| 2 | Cascading message delivered | `[IntegrationTest]` | `HiveMessageHandler<A, B>` returns B. Assert B is received by its handler. |
| 3 | Durable outbox delivers after handler commits | `[IntegrationTest]` | `[Transactional]` handler writes to DB and returns cascading message. Assert message arrives after commit. |
| 4 | Multi-broker routing | `[IntegrationTest]` | Publish to primary and named broker. Assert messages arrive at correct queues. |
| 5 | Full shutdown under load | `[IntegrationTest]` | Publish continuous stream of messages, trigger shutdown mid-stream. Assert: all in-flight messages complete, no messages lost, service exits cleanly. |

---

## 15. Implementation Phases

### Phase 1: Core (MVP)

- `MessagingExtensionBase.cs`, `MessagingExtension.cs`, `MessagingSendExtension.cs`, `Startup.cs`, `HiveMessagingBuilder`, `HiveMessagingSendBuilder`
- RabbitMQ transport with `RabbitMqTransportBuilder`
- System.Text.Json serialization (default)
- Configuration options with FluentValidation
- `HiveMessageHandler<T>` and `HiveMessageHandler<T, TResponse>` base classes
- `MessagingMeter` with handler and send telemetry
- `ReadinessMiddleware` + `MessageHandlerMiddleware` (instruments all handlers)
- Graceful shutdown integration with Hive `StopAsync()` lifecycle
- RabbitMQ health checks (primary + named brokers)
- Tests: sections 14.1 (readiness gate), 14.2 (graceful shutdown), 14.3 (handler hooks), 14.4 (telemetry), 14.5 (configuration), 14.6 (health checks), 14.7 #1-2 (basic e2e)

### Phase 2: Durability (`Hive.Messaging.EntityFrameworkCore`)

- Separate NuGet package with `DurabilityBuilder` and `DurabilityOptions`
- EF Core inbox/outbox via `WolverineFx.EntityFrameworkCore`
- `[Transactional]` handler support
- `.WithDurability()` extension method on `HiveMessagingBuilder`
- Tests: section 14.2 #5 (outbox flush on shutdown), 14.7 #3 (durable outbox e2e)

### Phase 3: Serialization

- MessagePack support
- Protobuf support
- Per-endpoint serialization configuration

### Phase 4: Testing

- `Hive.Messaging.Testing` with in-memory transport helpers
- Test session tracking utilities
- Tests: section 14.7 #4 (multi-broker), 14.7 #5 (full shutdown under load)

### Phase 5: Additional Transports (future)

- Azure Service Bus
- Kafka
