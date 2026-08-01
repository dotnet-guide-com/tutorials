# ASP.NET Core Output Cache — Minimal Invalidation Sample

A focused .NET 10 companion demonstrating how an ASP.NET Core Minimal API
caches GET responses, varies cache entries by request inputs, and evicts stale
responses after writes.

## Full tutorial

[ASP.NET Core Caching: Output Cache, Redis & Invalidation Strategies That Actually Work](https://www.dotnet-guide.com/tutorials/aspnet-core/caching-outputcache-redis-invalidation/)

## Framework note

The tutorial explains ASP.NET Core 8.

This companion targets .NET 10 so it can use the DOTNET GUIDE repository's
current SDK and CI toolchain. It demonstrates the same foundational Output Cache
policy, variation, tagging, invalidation, Minimal API, and integration-testing
patterns.

## What this sample demonstrates

- `AddOutputCache` and `UseOutputCache`;
- named list and detail policies;
- five- and ten-minute expirations;
- explicit variation by `category` and `sort`;
- explicit variation by route value `id`;
- one shared `products` tag;
- cached list and detail responses;
- tag eviction after POST, PUT, and DELETE;
- deterministic cache-hit verification;
- integration testing with `WebApplicationFactory<Program>`.

## Important query-key note

ASP.NET Core's default Output Cache key includes the full URL, including the
query string.

This sample uses:

```csharp
.SetVaryByQuery("category", "sort")
```

to intentionally restrict variation to the query parameters that affect the
response. Unrelated parameters such as tracking values do not create extra
cache entries.

## Important boundary

This repository sample intentionally does not reproduce the complete tutorial.

The full tutorial also covers:

- Redis-backed Output Cache;
- multi-instance deployment;
- `IDistributedCache`;
- key-pattern invalidation;
- version tokens;
- event-driven invalidation;
- background workers;
- stampede protection;
- jittered TTLs;
- cache metrics and debug headers;
- EF Core and SQLite;
- Docker and production guidance.

## Storage limitation

Both the product repository and Output Cache are stored in process memory.

Restarting the application resets:

- the product data;
- the cache;
- the origin-execution counters.

This sample does not demonstrate distributed caching.

## Prerequisite

- .NET 10 SDK

Check:

```powershell
dotnet --version
```

## Restore, build, and test

```powershell
dotnet restore `
  .\OutputCacheCatalogMinimal.slnx

dotnet build `
  .\OutputCacheCatalogMinimal.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\OutputCacheCatalogMinimal.slnx `
  --configuration Release `
  --no-build
```

## Run

```powershell
dotnet run `
  --project .\src\OutputCacheCatalogMinimal\OutputCacheCatalogMinimal.csproj `
  --urls http://localhost:5132
```

## Endpoints

```text
GET    /
GET    /products
GET    /products/{id}
POST   /products
PUT    /products/{id}
DELETE /products/{id}
```

## Cache policies

```text
ProductList
  TTL: 5 minutes
  Varies by: category, sort
  Tag: products

ProductDetail
  TTL: 10 minutes
  Varies by: route id
  Tag: products
```

## Why responses include OriginExecution

The diagnostic value proves whether the endpoint handler ran.

A cached second response retains the earlier execution value, showing that
Output Cache returned the stored response without executing the handler again.

This field is sample-only scaffolding, not a production API contract.

## Project structure

```text
OutputCacheCatalogMinimal.slnx
README.md
src/
└── OutputCacheCatalogMinimal/
    ├── OutputCacheCatalogMinimal.csproj
    └── Program.cs
tests/
└── OutputCacheCatalogMinimal.Tests/
    ├── OutputCacheCatalogMinimal.Tests.csproj
    └── OutputCacheTests.cs
```

## Deliberately omitted

- Redis;
- distributed caching;
- EF Core;
- SQLite;
- Docker;
- event-driven invalidation;
- version tokens;
- key scans;
- per-product dynamic tags;
- cache metrics;
- authentication;
- production deployment.

These are covered by the full tutorial.

## Verification

- Companion target framework: .NET 10
- Tutorial framework: .NET 8
- External services required: none
- Redis required: no
- Database required: no
- API keys required: none
- Expected integration tests: 8
- Last reviewed: 2026-08-01

This sample is educational and should be reviewed before production use.