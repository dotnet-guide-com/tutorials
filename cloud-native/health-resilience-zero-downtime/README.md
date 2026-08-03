# ASP.NET Core Health, Resilience, and Shutdown Readiness

A focused .NET 10 companion demonstrating tagged liveness and readiness checks,
structured health responses, readiness draining during shutdown, a typed
`HttpClient`, retries, a circuit breaker, an attempt timeout, and deterministic
integration tests.

## Full tutorial

[.NET 8 Cloud-Native: Health Probes, Polly Resilience & Zero-Downtime Kubernetes Deployments](https://www.dotnet-guide.com/tutorials/cloud-native/health-resilience-zero-downtime/)

## Framework note

The tutorial explains cloud-native ASP.NET Core patterns on .NET 8.

This companion targets .NET 10 so it can use the repository's current SDK and
CI workflow. The tagged health-check, shutdown-readiness, typed-HttpClient,
retry, circuit-breaker, timeout, and integration-testing concepts demonstrated
here are the same core patterns.

## What this sample demonstrates

- `live` and `ready` health-check tags;
- `/health/live`;
- `/health/ready`;
- `/health`;
- structured JSON health responses;
- process liveness independent from traffic readiness;
- a hosted service that marks readiness unavailable during shutdown;
- `Microsoft.Extensions.Http.Resilience`;
- one custom resilience handler;
- retrying HTTP 503 responses;
- not retrying HTTP 400 responses;
- a circuit breaker;
- an attempt timeout;
- deterministic attempt counts;
- six integration tests.

## Health semantics

```text
Normal:
  liveness  -> Healthy
  readiness -> Healthy

Draining:
  liveness  -> Healthy
  readiness -> Unhealthy
```

Liveness doesn't call an external dependency.

Readiness represents whether this instance should receive new traffic.

## Resilience pipeline

```text
typed HttpClient
  -> retry
  -> circuit breaker
  -> attempt timeout
  -> simulated payment handler
```

The retry and circuit-breaker strategies handle HTTP 503 only.

HTTP 400 is returned after one attempt.

## Important POST retry warning

The sample uses a POST-shaped authorization call because it makes the retry
behavior easy to understand.

The handler is entirely in process and causes no external side effect.

Do not blindly retry a real payment POST. Use an idempotency key or another
reviewed idempotency contract.

## Deterministic dependency simulation

`SimulatedPaymentHandler` doesn't use the network.

It returns a requested number of temporary failures and then succeeds.

This keeps the resilience tests:

- fast;
- infrastructure-free;
- deterministic;
- safe for CI.

It is not a production payment client or service-virtualization platform.

## Prerequisite

- .NET 10 SDK

## Restore, build, and test

```powershell
dotnet restore `
  .\ResilientOrdersMinimal.slnx

dotnet build `
  .\ResilientOrdersMinimal.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\ResilientOrdersMinimal.slnx `
  --configuration Release `
  --no-build
```

## Run

```powershell
dotnet run `
  --project .\src\ResilientOrdersMinimal\ResilientOrdersMinimal.csproj `
  --urls http://localhost:5156
```

## Health endpoints

```text
http://localhost:5156/health/live
http://localhost:5156/health/ready
http://localhost:5156/health
```

## Retry example

```powershell
Invoke-WebRequest `
  -Method Post `
  -Uri "http://localhost:5156/api/orders/42/authorize?failuresBeforeSuccess=2&failureStatusCode=503"
```

Expected:

```text
Succeeded: true
Attempts: 3
CircuitOpen: false
```

## Non-retriable example

Restart the app, then run:

```powershell
Invoke-WebRequest `
  -Method Post `
  -SkipHttpErrorCheck `
  -Uri "http://localhost:5156/api/orders/43/authorize?failuresBeforeSuccess=10&failureStatusCode=400"
```

Expected:

```text
HTTP 400
Attempts: 1
CircuitOpen: false
```

## Attempt-timeout example

The handler records one attempt, waits for a delay that exceeds the configured
two-second attempt timeout, and returns HTTP 504 Gateway Timeout.

Timeout exceptions are deliberately not retried in this sample. Only HTTP 503
responses trigger the retry strategy.

The delay exists solely for deterministic education and testing.

Restart the app, then run:

```powershell
Invoke-WebRequest `
  -Method Post `
  -SkipHttpErrorCheck `
  -Uri "http://localhost:5156/api/orders/45/authorize?failuresBeforeSuccess=0&failureStatusCode=503&delayMilliseconds=2500"
```

Expected:

```text
HTTP 504
Attempts: 1
CircuitOpen: false
```

Do not treat this timeout handling as a production payment policy.

## Circuit-breaker example

Restart the app and run this twice immediately:

```powershell
Invoke-WebRequest `
  -Method Post `
  -SkipHttpErrorCheck `
  -Uri "http://localhost:5156/api/orders/44/authorize?failuresBeforeSuccess=20&failureStatusCode=503"
```

First request:

```text
Attempts: 3
CircuitOpen: false
```

Second request:

```text
Attempts: 0
CircuitOpen: true
```

## Shutdown boundary

`ShutdownReadinessService` changes traffic readiness when application shutdown
begins.

It doesn't:

- remove a Kubernetes endpoint;
- delay SIGTERM;
- wait for a load balancer;
- guarantee zero downtime.

Those behaviors depend on the deployment platform and its probe, lifecycle, and
termination settings.

## Project structure

```text
ResilientOrdersMinimal.slnx
README.md
src/
`-- ResilientOrdersMinimal/
    |-- ResilientOrdersMinimal.csproj
    |-- Program.cs
    |-- Health/
    |   |-- HealthResponseWriter.cs
    |   |-- TrafficReadinessHealthCheck.cs
    |   `-- TrafficReadinessState.cs
    |-- Hosting/
    |   `-- ShutdownReadinessService.cs
    `-- Payments/
        |-- PaymentGatewayClient.cs
        |-- PaymentSimulationState.cs
        `-- SimulatedPaymentHandler.cs
tests/
`-- ResilientOrdersMinimal.Tests/
    |-- ResilientOrdersMinimal.Tests.csproj
    `-- HealthAndResilienceTests.cs
```

## Deliberately omitted

- databases;
- queues;
- migrations;
- background order processing;
- Docker;
- Kubernetes;
- OpenTelemetry;
- load testing;
- secrets;
- production rollout automation.

These remain in the full tutorial or other focused samples.

## Verification

- Companion target framework: .NET 10
- Tutorial framework: .NET 8
- Resilience package: Microsoft.Extensions.Http.Resilience 10.8.0
- External services required: none
- Database required: none
- Container runtime required: none
- Expected tests: 6
- Last reviewed: 2026-08-03

This sample is educational and should be reviewed against the semantics of the
real downstream operation and deployment platform before production use.