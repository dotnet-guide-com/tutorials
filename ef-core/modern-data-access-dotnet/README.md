# EF Core Relationship Loading & Query Shapes

A focused EF Core companion showing how relationship-loading choices change
database command shape.

## Full tutorial

[EF Core 8 Fundamentals: Modern Data Access with .NET 8](https://www.dotnet-guide.com/tutorials/ef-core/modern-data-access-dotnet/)

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
Category -> TodoItem
TodoItem <-> Tag
      ↓
intentional N+1 baseline
      ↓
Include
      ↓
filtered Include
      ↓
AsSplitQuery
      ↓
explicit loading
```

## Why SQLite?

The sample uses one open SQLite in-memory connection so it can demonstrate real
relational behavior without credentials, Docker, or a database server.

SQLite is still not equivalent to SQL Server or PostgreSQL.

Provider-specific production behavior should be tested against the actual
production provider.

## N+1 baseline

The sample intentionally includes an inefficient pattern:

```text
1 query for todos
+ 1 category query per todo
```

With three seeded todos, that is:

```text
4 SELECT commands
```

This is an anti-pattern included for comparison, not a recommendation.

## Eager loading

When the operation knows it needs the category relationship, the companion uses:

```csharp
Include(todo => todo.Category)
```

For this exact SQLite/EF Core 10.0.11 query shape, that produces one SELECT.

Do not generalize this count to arbitrary relationship graphs.

## Filtered Include

Todo 1 has two tags:

```text
urgent
planning
```

The filtered Include deliberately loads only:

```text
urgent
```

The scenario uses `AsNoTracking` so prior tracked entities cannot change the
filtered navigation through relationship fix-up.

## Split query

The nested graph:

```text
Category -> Todos -> Tags
```

is loaded with:

```csharp
AsSplitQuery()
```

For this pinned sample it issues three SELECT commands.

Split queries aren't universally faster. They trade JOIN result size for
multiple commands/round trips.

## Explicit loading

The explicit-load scenario first loads a TodoItem without its Category.

It then calls:

```csharp
context.Entry(todo)
    .Reference(item => item.Category)
    .LoadAsync(...)
```

The test proves the reference changes from not loaded to loaded.

Use explicit loading when the relationship is conditional rather than always
required.

## No lazy-loading proxies

This companion intentionally does not install:

```text
Microsoft.EntityFrameworkCore.Proxies
```

Lazy loading can hide extra database round trips, making N+1 behavior harder to
see in a learning sample.

## Deterministic output

```text
EF Core Relationship Loading Lab
Seeded graph: categories=2, todos=3, tags=3
N+1 baseline: todos=3, SELECTs=4
Eager Include: todos=3, SELECTs=1
Split graph: categories=2, todos=3, tags=3, SELECTs=3
Explicit reference load: todo=Prepare release, category=Work, SELECTs=2
```

## Restore, build, and test

```powershell
dotnet restore `
  .\EfCoreRelationshipsMinimal.slnx

dotnet build `
  .\EfCoreRelationshipsMinimal.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\EfCoreRelationshipsMinimal.slnx `
  --configuration Release `
  --no-build
```

## Run

```powershell
dotnet run `
  --project .\src\EfCoreRelationshipsMinimal\EfCoreRelationshipsMinimal.csproj `
  --configuration Release `
  --no-build
```

## Deliberately omitted

- web API CRUD;
- migrations;
- owned/complex types;
- JSON;
- compiled queries;
- ExecuteUpdate/Delete;
- concurrency;
- transactions;
- retries;
- SQL Server;
- PostgreSQL;
- lazy-loading proxies;
- raw SQL;
- Testcontainers;
- benchmarks.

The complete tutorial covers the broader EF Core fundamentals.

## Verification

- Target framework: .NET 10
- EF Core provider: SQLite 10.0.11
- Direct application packages: 1
- Tests: 8
- External services: none
- Generated database files: none
- Benchmark claims: none
- Last reviewed: 2026-08-14