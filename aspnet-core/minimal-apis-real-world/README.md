# Real-World Minimal API Pipeline

A focused .NET 10 companion demonstrating endpoint filters, FluentValidation,
URL-segment API versioning, typed results, and partitioned rate limiting in an
ASP.NET Core Minimal API.

## Full tutorial

[ASP.NET Core / Minimal APIs in the Real World: Filters, Validation, Versioning & Rate Limiting](https://www.dotnet-guide.com/tutorials/aspnet-core/minimal-apis-real-world/)

## Framework note

The tutorial explains ASP.NET Core 8.

This companion targets .NET 10 so it can use the DOTNET GUIDE repository's
current SDK and CI toolchain. The endpoint-filter, FluentValidation,
URL-versioning, typed-result, and rate-limiting patterns demonstrated here are
the same core ASP.NET Core concepts.

## What this sample demonstrates

- route groups;
- endpoint filters;
- validation before handler execution;
- RFC 7807 validation responses;
- API v1 and v2 using URL segments;
- different response contracts over one repository;
- typed HTTP results;
- a fixed-window write limiter;
- independent rate-limit partitions;
- integration tests with `WebApplicationFactory<Program>`.

## Request pipeline

```text
request
  -> version selection
  -> rate limiting
  -> timing filter
  -> validation filter
  -> endpoint handler
  -> typed response
```

## Versions

### V1

```text
GET  /api/v1/orders
GET  /api/v1/orders/{id}
POST /api/v1/orders
```

V1 returns item names directly.

### V2

```text
GET /api/v2/orders?page=1&pageSize=2
GET /api/v2/orders/{id}
```

V2 returns a paginated envelope and exposes item counts.

## Demonstration rate-limit partition

The create endpoint permits two requests per minute for each distinct
`X-Client-Id` value.

This header is used only to make partition behavior easy to test.

It is not authentication and must not be trusted as a production identity.
Production partition keys should come from validated identities, API keys,
tenants, or trusted network signals.

## Important boundary

This repository sample intentionally does not reproduce the complete tutorial.

The full tutorial also covers:

- JWT authentication;
- scope and role authorization;
- OpenAPI and Swagger;
- response and output caching;
- structured logging;
- OpenTelemetry;
- health checks;
- Kestrel tuning;
- Docker;
- production deployment.

Dedicated repository companions already cover API security and Output Cache.

## Persistence limitation

Orders are stored only in process memory.

Restarting the application restores the three seeded orders and resets all
rate-limit partitions.

## Prerequisite

- .NET 10 SDK

Check:

```powershell
dotnet --version
```

## Restore, build, and test

```powershell
dotnet restore `
  .\MinimalApiPipeline.slnx

dotnet build `
  .\MinimalApiPipeline.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\MinimalApiPipeline.slnx `
  --configuration Release `
  --no-build
```

## Run

```powershell
dotnet run `
  --project .\src\MinimalApiPipeline\MinimalApiPipeline.csproj `
  --urls http://localhost:5136
```

## Example valid request

```powershell
$headers = @{
  "X-Client-Id" = "powershell-demo"
}

$body = @{
  customerId = "customer-400"
  items = @(
    "microphone"
  )
} | ConvertTo-Json

Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5136/api/v1/orders" `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $body
```

## Project structure

```text
MinimalApiPipeline.slnx
README.md
src/
`-- MinimalApiPipeline/
    |-- MinimalApiPipeline.csproj
    `-- Program.cs
tests/
`-- MinimalApiPipeline.Tests/
    |-- MinimalApiPipeline.Tests.csproj
    `-- MinimalApiPipelineTests.cs
```

## Deliberately omitted

- authentication;
- authorization;
- Swagger;
- OpenAPI generation;
- caching;
- database persistence;
- Redis;
- OpenTelemetry exporters;
- Docker;
- cloud deployment;
- production identity partitioning.

These are covered by the full tutorial or another focused sample.

## Verification

- Companion target framework: .NET 10
- Tutorial framework: .NET 8
- External services required: none
- Database required: none
- API keys required: none
- Expected integration tests: 8
- Last reviewed: 2026-08-01

This sample is educational and should be reviewed and load-tested before
production use.