# EF Core SQLite vs InMemory Relational-Behavior Contrast

A focused .NET 10 / EF Core 10 test-provider lab showing four concrete
relational behaviors that differ between SQLite in-memory and the EF Core
InMemory provider.

## Full tutorial

[EF Core Testing: SQLite In-Memory vs InMemory Provider — Which One Can You Actually Trust?](https://www.dotnet-guide.com/tutorials/ef-core/testing-sqlite-inmemory-vs-inmemory/)

## Version baseline

```text
.NET 10
Microsoft.EntityFrameworkCore.Sqlite 10.0.11
Microsoft.EntityFrameworkCore.InMemory 10.0.11
```

Provider behavior is version-sensitive. The assertions in this sample are
reviewed against this pinned baseline.

## Why InMemory appears in this repository

Microsoft discourages using the EF Core InMemory provider as a general database
fake for relational applications.

It is included here because the tutorial specifically compares its behavior
with SQLite.

This is a contrast sample, not a recommendation to introduce InMemory into new
integration-test suites.

## What the sample compares

```text
                   InMemory          SQLite :memory:
Unique index       accepts           rejects
Foreign key        accepts           rejects
Transaction        unsupported       rollback works
DB cascade         orphan remains    child removed
```

## One shared model

Both providers use exactly the same:

```text
Project
TaskItem
unique (Name, OwnerId) index
Project -> Tasks foreign key
DeleteBehavior.Cascade
```

Only the provider changes.

## SQLite connection lifetime

The SQLite helper opens one low-level in-memory connection and leaves it open
until the scenario finishes.

Closing the connection destroys the in-memory database.

## Foreign keys

The sample uses:

```text
Data Source=:memory:;Foreign Keys=True
```

to make FK enforcement explicit.

The default EF Core SQLite native bundle currently enables foreign keys by
default, so a manual PRAGMA is not universally required with the standard
bundle.

## Database-side cascade

The cascade scenario intentionally deletes a Project from a fresh context that
does not load the child TaskItem.

That distinction matters.

EF Core can apply client-side cascade behavior to dependents already tracked in
a DbContext. The test here specifically asks whether the backing database/store
enforces the FK cascade when the dependent isn't tracked.

## Transactions

The EF InMemory provider doesn't support transactions.

By default, attempting to start one throws.

EF can be configured to ignore the transaction warning, but doing so still
does not provide rollback semantics.

SQLite executes a real transaction in this sample.

## Concurrency is deliberately not part of the contrast

Current EF Core InMemory code checks configured concurrency tokens and can
throw `DbUpdateConcurrencyException`.

The full website tutorial must not describe current InMemory as simply ignoring
concurrency tokens.

SQLite still differs from SQL Server for database-generated rowversion-style
tokens.

## Query translation is also not reduced to a one-line matrix

InMemory and relational providers use different query pipelines and may fail or
behave differently.

Modern EF Core doesn't generally turn arbitrary non-translatable expressions
into silent client evaluation.

Provider-specific query behavior should be tested against the provider that
matters.

## SQLite is still a fake for another production engine

SQLite relational behavior is more realistic than EF InMemory for these four
cases, but SQLite is not SQL Server or PostgreSQL.

Differences include:

- case sensitivity/collation;
- SQL dialect;
- provider-specific methods;
- native data types;
- migrations;
- concurrency-token generation;
- JSON/provider extensions.

Use the real production database in automated tests when those differences
matter.

## Deterministic output

```text
EF Core Test Provider Contrast Lab
Unique index: InMemory=accepted | SQLite=rejected
Foreign key: InMemory=accepted | SQLite=rejected
Transaction: InMemory=unsupported | SQLite=rollback-ok
Database cascade: InMemory=orphan-remains | SQLite=child-removed
Guidance: relational behavior -> production provider when practical; SQLite remains a test double.
```

## Restore, build, test

```powershell
dotnet restore `
  .\EfCoreProviderContrastMinimal.slnx

dotnet build `
  .\EfCoreProviderContrastMinimal.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\EfCoreProviderContrastMinimal.slnx `
  --configuration Release `
  --no-build
```

## Run

```powershell
dotnet run `
  --project .\src\EfCoreProviderContrastMinimal\EfCoreProviderContrastMinimal.csproj `
  --configuration Release `
  --no-build
```

## Deliberately omitted

- web API;
- concurrency-provider matrix;
- SQL Server;
- PostgreSQL;
- Testcontainers;
- migrations;
- raw SQL;
- JSON;
- temporal tables;
- performance benchmarking.

## Verification

- Target framework: .NET 10
- EF Core baseline: 10.0.11
- Direct app packages: 2
- Tests: 8
- External services: none
- Physical database files: none
- Performance claims: none
- Last reviewed: 2026-08-17