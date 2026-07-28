# Transactional Outbox with EF Core &mdash; Minimal SQLite Sample

Full tutorial: [Transactional Outbox Pattern in .NET with EF Core (.NET 10): Fix the Dual-Write Problem](https://www.dotnet-guide.com/tutorials/distributed-systems/transactional-outbox-ef-core/)

## What this sample demonstrates

- .NET 10 console application
- EF Core 10 with SQLite relational storage
- One `Order` business entity
- One `OrderPlaced` integration event
- One `OutboxMessage` outbox row
- One atomic `SaveChangesAsync` writing order + outbox together
- One broker-neutral `IPublisher` interface
- One `InMemoryPublisher` for deterministic demonstration
- One one-shot `OutboxRelay` that processes pending messages
- xUnit v3 tests: atomic write, relay processing, no duplicate dispatch
- Normal CI execution (no Docker, no external services)

## Architecture

```
OrderService
   |
   |-- Order
   |-- OutboxMessage
          |
          |-- one SaveChangesAsync
                    |
                    v
                OutboxRelay
                    |
                    v
             InMemoryPublisher
```

## File structure

```
distributed-systems/
+-- transactional-outbox-ef-core/
    |-- TransactionalOutboxMinimal.slnx
    |-- README.md
    |-- src/
    |   +-- TransactionalOutboxMinimal/
    |       |-- TransactionalOutboxMinimal.csproj
    |       |-- Program.cs
    |       |-- Data/
    |       |   +-- OrdersDbContext.cs
    |       |-- Domain/
    |       |   +-- Order.cs
    |       |-- Messaging/
    |       |   |-- OrderPlaced.cs
    |       |   |-- OutboxMessage.cs
    |       |   |-- IPublisher.cs
    |       |   |-- InMemoryPublisher.cs
    |       |   +-- OutboxRelay.cs
    |       +-- Services/
    |           +-- OrderService.cs
    +-- tests/
        +-- TransactionalOutboxMinimal.Tests/
            |-- TransactionalOutboxMinimal.Tests.csproj
            +-- OutboxFlowTests.cs
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

No Docker, PostgreSQL, RabbitMQ, or any external service is required.

## Run

```powershell
cd distributed-systems\transactional-outbox-ef-core
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet run --project src\TransactionalOutboxMinimal --configuration Release --no-build
```

## Expected output

```
Order committed: 1
Outbox rows committed: 1
Pending before relay: 1
Published by relay: 1
Published message IDs: 1
Pending after relay: 0
Processed outbox rows: 1
Second relay run published 0 messages (expected)
Deleted temp DB: /tmp/transactional-outbox-....
```

## Verify

The output confirms:

1. One `Order` and one `OutboxMessage` are written by a single `SaveChangesAsync`.
2. The relay finds exactly one pending row and publishes it.
3. The relay marks the row processed.
4. A second relay run publishes zero rows &mdash; processed rows are not republished.
5. The temporary SQLite database is cleaned up.

## Important boundary

This sample demonstrates the **normal successful path only**. It intentionally omits:

- PostgreSQL, Npgsql, and provider-specific locking
- RabbitMQ or any external message broker
- ASP.NET Core API endpoints
- Docker Compose
- Concurrent dispatchers and `FOR UPDATE SKIP LOCKED`
- Inbox tables and duplicate-consumer handling
- `SaveChangesInterceptor`
- Aggregate domain-event infrastructure
- Retries, poison-message quarantine, and dead-letter storage
- OpenTelemetry, trace propagation, and health checks
- Change data capture (CDC) and Debezium
- Testcontainers
- Production deployment

The full tutorial remains the authoritative source for all production outbox behavior.

## Delivery semantics

The transactional outbox provides **eventual at-least-once publication**, not exactly-once delivery.

A relay may publish a message successfully and crash before marking the row processed. That row will be published again. **Production consumers must be idempotent.**

Inbox implementation, idempotent consumer guidance, and crash recovery patterns are covered in the complete tutorial.

## Verification details

| Item | Value |
| --- | --- |
| Target framework | `net10.0` |
| `Microsoft.EntityFrameworkCore` | 10.0.10 |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.10 |
| `xunit.v3` | 3.2.2 |
| `xunit.runner.visualstudio` | 3.1.5 |
| `Microsoft.NET.Test.Sdk` | 17.14.1 |
| Database | Temporary SQLite file (created per run, deleted on exit) |
| External services required | None |
| Containers required | None |
| API keys required | None |
| Last reviewed | July 28, 2026 |
| Release build | Verified |
| Tests | Verified (3/3 passing) |
| Console run | Verified |

## License

Sample code in this folder is available under the [MIT License](../LICENSE).