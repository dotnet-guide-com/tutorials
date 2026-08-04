# Polly Fallback and Concurrency Isolation

A focused .NET 10 companion demonstrating a generic Polly v8 pipeline with
explicit stale-cache fallback, a bounded timeout, outbound concurrency
isolation, strategy event counters, and deterministic integration tests.

## Full tutorial

[Polly Resilience (Polly v8): Timeouts, Retries, Circuits, Bulkheads, Hedging](https://www.dotnet-guide.com/tutorials/cloud-native/polly-resilience/)

## Framework note

The tutorial explains Polly v8 resilience patterns on .NET 8.

This companion targets .NET 10 so it can use the DOTNET GUIDE repository's
current SDK and CI workflow. The fallback, timeout, concurrency-limiter,
cancellation, stale-cache, and integration-testing concepts demonstrated here
are the same Polly v8 patterns.

## Why this sample is narrow

The repository already contains:

```text
cloud-native/health-resilience-zero-downtime/
```

That sample covers retries, attempt timeouts, circuit breaking, health checks,
and shutdown readiness.

This companion concentrates on different Polly strategies:

- fallback;
- graceful degradation;
- concurrency isolation;
- strategy callback visibility.

## Packages

```text
Polly                 8.7.0
Polly.RateLimiting    8.7.0
```

The rate-limiter strategy is packaged separately from Polly's core strategies.

## Pipeline order

```text
fallback
  -> timeout
  -> concurrency limiter
  -> catalog dependency
```

Strategies execute in registration order from outermost to innermost.

Fallback is outermost so it can replace:

- dependency failures;
- timeout rejections;
- concurrency-limiter rejections.

## Concurrency settings

```text
PermitLimit = 1
QueueLimit  = 0
```

Only one protected dependency operation can execute at a time.

A second concurrent operation is rejected immediately and receives the stale
fallback snapshot.

This is a deliberately small teaching limit, not a production capacity
recommendation.

## Fallback transparency

Fallback responses include:

```text
X-Resilience-Fallback: true
X-Resilience-Reason: dependency-failure | timeout | bulkhead-rejected
```

The JSON payload also contains:

```text
source: stale-cache
isStale: true
degradedReason: ...
```

The sample does not disguise stale content as a fresh response.

## Cancellation boundary

The timeout strategy can stop the simulated slow dependency because it passes
the strategy cancellation token to `Task.Delay`.

Caller cancellation is not included in the fallback predicate and must remain a
cancellation.

The integration suite proves caller cancellation propagates without producing
stale fallback data or incrementing resilience counters.

## Deterministic simulation

The dependency modes are:

```text
live
failure
slow
hold
```

`hold` exists for the concurrency integration test.

The dependency performs no network request.

## Prerequisite

- .NET 10 SDK

## Restore, build, and test

```powershell
dotnet restore `
  .\PollyCatalogResilience.slnx

dotnet build `
  .\PollyCatalogResilience.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\PollyCatalogResilience.slnx `
  --configuration Release `
  --no-build
```

## Run

```powershell
dotnet run `
  --project .\src\PollyCatalogResilience\PollyCatalogResilience.csproj `
  --urls http://localhost:5160
```

## Live response

```powershell
Invoke-RestMethod `
  -Uri "http://localhost:5160/api/catalog"
```

## Dependency-failure fallback

```powershell
$response = Invoke-WebRequest `
  -Uri "http://localhost:5160/api/catalog?mode=failure"

$response.Headers
$response.Content | ConvertFrom-Json
```

## Timeout fallback

```powershell
$response = Invoke-WebRequest `
  -Uri "http://localhost:5160/api/catalog?mode=slow&delayMilliseconds=1000"

$response.Headers
$response.Content | ConvertFrom-Json
```

## Strategy counters

```powershell
Invoke-RestMethod `
  -Uri "http://localhost:5160/resilience/status"
```

## Cache boundary

The stale snapshot is fixed demonstration data.

A production cache requires decisions about:

- freshness limits;
- invalidation;
- refresh;
- distributed consistency;
- tenant isolation;
- authorization;
- observability.

## Telemetry boundary

The in-memory counters make Polly callbacks visible to the sample tests.

They are not an observability backend.

Use a reviewed telemetry pipeline for production metrics and traces.

## Project structure

```text
PollyCatalogResilience.slnx
README.md
src/
`-- PollyCatalogResilience/
    |-- PollyCatalogResilience.csproj
    |-- Program.cs
    |-- Models/
    |   `-- CatalogModels.cs
    |-- Resilience/
    |   |-- CatalogPipelineFactory.cs
    |   |-- CatalogResilienceService.cs
    |   `-- ResilienceTelemetry.cs
    `-- Services/
        |-- CatalogCache.cs
        |-- CatalogDependency.cs
        `-- CatalogHoldGate.cs
tests/
`-- PollyCatalogResilience.Tests/
    |-- PollyCatalogResilience.Tests.csproj
    `-- CatalogResilienceTests.cs
```

## Deliberately omitted

- retries;
- circuit breakers;
- hedging;
- external HTTP calls;
- distributed caching;
- OpenTelemetry;
- chaos testing;
- load testing;
- Docker;
- Kubernetes;
- production tuning.

These remain in the full tutorial or other focused companions.

## Verification

- Companion target framework: .NET 10
- Tutorial framework: .NET 8
- Polly: 8.7.0
- Polly.RateLimiting: 8.7.0
- Timeout: 500 milliseconds
- Permit limit: 1
- Queue limit: 0
- External services required: none
- Database required: none
- Container runtime required: none
- Expected tests: 6
- Last reviewed: 2026-08-04

This sample is educational. Production limits and fallback rules must be based
on measured capacity, data freshness requirements, and dependency contracts.