# EF Core Hot-Path Queries & Set-Based Operations

A focused EF Core companion demonstrating a lean read path, a stable compiled
query, a named soft-delete query filter, and server-side set-based updates and
deletes without external database infrastructure.

## Full tutorial

[EF Core Advanced Modeling & Performance: Owned Types, Converters, JSON/Temporal Tables, Compiled Queries](https://www.dotnet-guide.com/tutorials/ef-core/advanced-modeling-performance/)

## Version note

The full tutorial was written around .NET 8 / EF Core 8.

This companion targets:

```text
.NET 10
EF Core 10
Microsoft.EntityFrameworkCore.Sqlite 10.0.11
```

because that is the current DOTNET GUIDE repository baseline.

## Focus

```text
SQLite in-memory
  -> named soft-delete filter
  -> DTO projection
  -> AsNoTracking
  -> EF.CompileAsyncQuery
  -> ExecuteUpdateAsync
  -> ExecuteDeleteAsync
  -> fresh-context verification
```

This sample is intentionally not a benchmark.

## Why SQLite?

SQLite gives the companion a real relational database without:

- credentials;
- a server;
- Docker;
- Testcontainers;
- generated database files.

The low-level in-memory connection remains open for the lifetime of each
scenario so multiple DbContext instances see the same database.

SQLite is still a different provider from SQL Server and PostgreSQL.

This sample does not validate provider-specific JSON, temporal-table,
concurrency-token, migration, query-plan, or batching behavior.

## Soft-delete filter

The companion targets EF Core 10 and uses a named query filter:

```csharp
entity.HasQueryFilter(
    "SoftDeleteFilter",
    product => !product.IsDeleted);
```

The purge path selectively disables it:

```csharp
IgnoreQueryFilters(
    ["SoftDeleteFilter"])
```

The EF Core 8 tutorial must continue to explain that older versions use one
combined filter expression when several filters apply to the same entity.

## Projection and tracking

The read path projects only:

```text
Sku
Name
PriceCents
StockQuantity
```

into `ProductSummary`.

It also uses `AsNoTracking` to make the read-only intent explicit.

The tests verify that no entity entries remain in the DbContext change tracker.

## Compiled query

The sample uses one:

```text
EF.CompileAsyncQuery
```

for a stable SKU lookup.

This does not mean every query should be manually compiled.

EF Core already caches ordinary queries by expression-tree shape. Explicit
compiled queries bypass the normal cache lookup and are intended for measured
hot paths with stable query shapes and simple scalar parameters.

The sample makes no fixed latency-savings claim.

## Set-based update

```text
ExecuteUpdateAsync
```

increments stock for all visible products below the threshold in one
set-based command.

The application does not load those products before updating them.

## Change-tracker caveat

`ExecuteUpdateAsync` does not synchronize entity instances that were already
tracked by the DbContext.

The test suite deliberately proves this behavior.

Use a clear context boundary, reload, or clear tracking when mixing set-based
operations with tracked entities.

## Set-based delete

The soft-deleted row is hidden by default.

The purge operation explicitly disables the named soft-delete filter and calls:

```text
ExecuteDeleteAsync
```

The affected-row count is asserted.

## Deterministic output

```text
EF Core Hot-Path Query Lab
Visible products: 4
Compiled lookup: SKU-003 | Dock | 12900 cents
Restocked products: 3
Purged soft-deleted products: 1
Final rows: 4
```

## Restore, build, and test

```powershell
dotnet restore `
  .\EfCoreHotPathMinimal.slnx

dotnet build `
  .\EfCoreHotPathMinimal.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\EfCoreHotPathMinimal.slnx `
  --configuration Release `
  --no-build
```

## Run

```powershell
dotnet run `
  --project .\src\EfCoreHotPathMinimal\EfCoreHotPathMinimal.csproj `
  --configuration Release `
  --no-build
```

## Deliberately omitted

- SQL Server;
- PostgreSQL;
- temporal tables;
- JSON columns;
- owned/complex types;
- value converters;
- encryption;
- concurrency tokens;
- migrations;
- Testcontainers;
- split queries;
- logging/interceptors;
- benchmarking.

The complete tutorial covers the broader modeling and provider-specific
concepts.

## Verification

- Target framework: .NET 10
- EF Core provider: SQLite 10.0.11
- Direct application packages: 1
- Tests: 8
- External services: none
- Generated database files: none
- Benchmarks: none
- Last reviewed: 2026-08-14