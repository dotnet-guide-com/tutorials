# Bounded Background Job Queue

A focused ASP.NET Core Minimal API companion demonstrating a bounded
`Channel<T>`, asynchronous producer backpressure, a `BackgroundService`
consumer, fresh dependency-injection scopes per job, safe in-memory status
tracking, exception isolation, and cooperative cancellation.

## Full tutorial

[.NET 8 Background Jobs: IBackgroundTaskQueue, BackgroundService & Production-Ready Patterns](https://www.dotnet-guide.com/tutorials/dotnet-8-essentials/background-jobs-hostedservice-queues/)

## Framework note

The full tutorial is written for ASP.NET Core 8.

This repository companion targets .NET 10 because that is the current DOTNET
GUIDE sample SDK.

The core APIs demonstrated here are the same hosted-service and
`System.Threading.Channels` patterns.

## Flow

```text
POST /jobs/email
  -> validate
  -> create typed EmailJob
  -> register Queued state
  -> bounded Channel<EmailJob>
  -> BackgroundService
  -> fresh async DI scope
  -> scoped handler
  -> terminal status
```

## What this sample proves

- the queue is bounded;
- producers wait asynchronously when it is full;
- jobs are consumed in FIFO order;
- one consumer processes jobs sequentially;
- every job gets a fresh dependency-injection scope;
- one failed job does not stop the next job;
- host cancellation reaches the in-flight handler;
- status responses do not expose exception details;
- queue admission closes during worker shutdown.

## Non-durability boundary

The queue and status tracker live only in process memory.

A process restart loses:

- queued jobs;
- running state;
- completed status history.

`202 Accepted` means the job entered this process's in-memory queue.

It does not mean the job is durably stored or guaranteed to finish.

Use a durable broker or job framework when work must survive restarts.

## Typed jobs

The queue stores an `EmailJob` record rather than a delegate.

This keeps job payloads inspectable and avoids implying that delegates can be
serialized into Redis or a cloud queue.

A durable implementation would still need:

- a message contract;
- serialization;
- delivery acknowledgements;
- visibility or lease handling;
- retry classification;
- poison-message storage;
- idempotency.

## Shutdown model

This sample uses cancellation-first shutdown.

When stopping begins:

1. queue admission closes;
2. the worker token is canceled;
3. the in-flight handler receives cancellation;
4. queued items can remain unprocessed.

This is not a full drain or durable recovery implementation.

## API

### Enqueue

```http
POST /jobs/email
Content-Type: application/json

{
  "to": "reader@example.com",
  "subject": "Queue sample"
}
```

Returns:

```text
202 Accepted
Location: /jobs/{jobId}
```

### Job status

```http
GET /jobs/{jobId}
```

### Local queue status

```http
GET /jobs/queue
```

The queue-status endpoint is for demonstration and testing.

It is not an authenticated production admin API.

### Cancellation and concurrency

A producer waiting for bounded capacity uses the HTTP request cancellation
token. If the request is canceled before the channel accepts the job, the
temporary tracker registration is removed and the cancellation continues to
the caller.

Status snapshots returned by `GET /jobs/{jobId}` are synchronized
independently from the `ConcurrentDictionary` that stores tracker entries.
Each snapshot read acquires the per-entry lock, ensuring a consistent view of
terminal state after the worker transitions the job.

## Prerequisite

- .NET 10 SDK

## Restore, build, and test

```powershell
dotnet restore `
  .\BackgroundJobQueueMinimal.slnx

dotnet build `
  .\BackgroundJobQueueMinimal.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\BackgroundJobQueueMinimal.slnx `
  --configuration Release `
  --no-build
```

## Run

```powershell
dotnet run `
  --project .\src\BackgroundJobQueueMinimal\BackgroundJobQueueMinimal.csproj `
  --configuration Release `
  --no-build `
  --urls http://127.0.0.1:5096
```

## Project structure

```text
BackgroundJobQueueMinimal.slnx
README.md
src/
`-- BackgroundJobQueueMinimal/
    |-- BackgroundJobQueueMinimal.csproj
    |-- Program.cs
    |-- Jobs/
    |   |-- BackgroundJobModels.cs
    |   |-- IBackgroundJobQueue.cs
    |   |-- BoundedBackgroundJobQueue.cs
    |   |-- IJobTracker.cs
    |   |-- InMemoryJobTracker.cs
    |   `-- QueuedEmailWorker.cs
    `-- Services/
        |-- IEmailJobHandler.cs
        `-- FakeEmailJobHandler.cs
tests/
`-- BackgroundJobQueueMinimal.Tests/
    |-- BackgroundJobQueueMinimal.Tests.csproj
    `-- BackgroundJobQueueTests.cs
```

## Deliberately omitted

- retries;
- Polly;
- dead-letter storage;
- schedules;
- metrics exporters;
- health probes;
- durable brokers;
- databases;
- real email delivery;
- multiple workers;
- parallel processing.

## Verification

- Target framework: .NET 10
- Application NuGet dependencies: none
- Test count: 8
- External services: none
- Queue capacity: 4 by default
- Durability: in-memory only
- Last reviewed: 2026-08-06