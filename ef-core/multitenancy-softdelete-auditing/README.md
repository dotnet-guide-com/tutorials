# EF Core Tenant Isolation, Soft Delete &amp; Audit Guardrails &mdash; Minimal Companion

A focused .NET 10 / EF Core 10 companion to the EF Core 8-era tutorial:

**[EF Core 8 Multi-Tenancy: Tenant Isolation, Soft Deletes, Audit Trails &amp; Query Filters](https://www.dotnet-guide.com/tutorials/ef-core/multitenancy-softdelete-auditing/)**

## What this sample demonstrates

The full tutorial is deliberately broad (tenant resolution, global filters, soft delete,
audit metadata, background jobs, admin bypass, indexes, compiled queries, APIs, migrations,
testing, database-level isolation). Several of those topics (projection, compiled queries,
`ExecuteUpdate`/`ExecuteDelete`, relationship loading, N+1, `Include`) are already covered by
other companions in this repository.

This companion answers one narrow question:

> How can EF Core make shared-table tenant reads fail closed, keep soft-delete bypass
> tenant-safe, reject cross-tenant writes, and prove those guardrails against one relational
> database containing multiple tenants at the same time?

Specifically it shows:

- A validated, trusted `TenantExecutionContext` that is injected into every `DbContext`.
- A fail-closed **named `TenantFilter`** that is always active (EF Core 10 named filters).
- A **named `SoftDeleteFilter`** that can be disabled selectively without ever touching the
  tenant filter.
- A composite primary key `TenantId + Id` so `alpha/1` and `beta/1` can coexist physically.
- A `SaveChangesInterceptor` (both sync and async paths) that:
  - overwrites the tenant on insert from the trusted scope,
  - rejects cross-tenant mutations,
  - stamps immutable creation audit fields and update audit fields,
  - converts `Remove()` into a narrow soft-delete update.
- Deterministic integration tests over one shared SQLite in-memory database where `alpha`
  and `beta` tenants live in the same physical database.

## Architecture

```text
already-authorized TenantExecutionContext
        |
        v
 EF Core 10 named TenantFilter
        +
 EF Core 10 named SoftDeleteFilter
        |
        v
 TenantId + Id composite key
        |
        v
 SaveChangesInterceptor
        |-- overwrite tenant on insert
        |-- reject foreign-tenant mutation
        |-- stamp audit metadata
        `-- convert Remove() to soft delete
        |
        v
 IgnoreQueryFilters(["SoftDeleteFilter"])
        |
        v
 SQLite in-memory shared alpha/beta database
        |
        v
 integration tests prove isolation
```

## Security design: fail closed

This sample intentionally does **not** use the tutorial's filter pattern:

```csharp
!hasTenant || entity.TenantId == tenantId
```

That expression is fail-open: a missing tenant context becomes a cross-tenant escape hatch.
Instead the companion requires:

- a non-empty tenant before a `DbContext` is ever created;
- every normal `DbContext` to belong to exactly one tenant;
- a tenant-equality filter that is always active;
- **no** `SystemTenantContext` that means "all tenants";
- **no** production helper that calls parameterless `IgnoreQueryFilters()`.

> `TenantExecutionContext` represents an already-authenticated and authorized tenant scope.
> Normalizing a string is not authorization.

## EF Core 8 vs EF Core 10 filter behavior

The website tutorial targets EF Core 8, where tenant isolation and soft deletion on the same
entity must be expressed as **one combined filter expression**. This repository companion
targets EF Core 10 and uses **two named filters** so the soft-delete filter can be bypassed on
its own while the tenant filter stays active:

```csharp
.HasQueryFilter("TenantFilter", task => task.TenantId == _tenantId)
.HasQueryFilter("SoftDeleteFilter", task => !task.IsDeleted)
```

Selective bypass:

```csharp
IgnoreQueryFilters([TenantDbContext.SoftDeleteFilterName])
```

## File structure

```text
ef-core/multitenancy-softdelete-auditing/
|-- EfCoreTenantGuardrailsMinimal.slnx
|-- README.md
|-- src/EfCoreTenantGuardrailsMinimal/
|   |-- EfCoreTenantGuardrailsMinimal.csproj
|   |-- Program.cs
|   |-- Context/TenantExecutionContext.cs
|   |-- Data/TenantDatabase.cs
|   |-- Data/TenantDbContext.cs
|   |-- Interceptors/TenantAuditInterceptor.cs
|   |-- Models/TenantTask.cs
|   |-- Queries/TenantQueries.cs
|   `-- Services/TenantWorkflow.cs
`-- tests/EfCoreTenantGuardrailsMinimal.Tests/
    |-- EfCoreTenantGuardrailsMinimal.Tests.csproj
    `-- TenantGuardrailTests.cs
```

## Prerequisites

- .NET 10 SDK (10.0.302 or later in the `10.0.x` band)

## Run

```bash
cd ef-core/multitenancy-softdelete-auditing

dotnet restore EfCoreTenantGuardrailsMinimal.slnx
dotnet test EfCoreTenantGuardrailsMinimal.slnx --configuration Release

dotnet run \
  --project src/EfCoreTenantGuardrailsMinimal/EfCoreTenantGuardrailsMinimal.csproj \
  --configuration Release
```

## Verify

The console app prints exactly:

```text
EF Core Tenant Isolation Lab
Visible tasks: alpha=1, beta=1
Same local ID: alpha=Alpha plan | beta=Beta plan
Alpha including deleted: 2, foreign tenants=0
Spoofed insert stamped tenant: alpha
Soft delete: visible alpha=1, physical alpha=3, deletedBy=alpha-admin
```

The test project runs 8 deterministic xUnit v3 tests covering:

- same local IDs isolated per tenant and physically coexisting;
- named soft-delete bypass preserving the tenant filter;
- spoofed insert tenant overwrite + audit stamping;
- soft delete preserving the row and stamping deletion metadata;
- modification preserving creation fields and stamping update audit;
- cross-tenant detached update rejected with `beta/1` unchanged;
- missing/blank tenant scope rejected;
- deterministic workflow summary.

## Important boundary (deliberate exclusions)

This sample begins **after** authentication and tenant authorization. It does not treat a raw
HTTP header or subdomain as authorization. Kept out so the sample stays small and honest:

- HTTP tenant headers and subdomain parsing;
- JWT / authentication and tenant membership lookup;
- platform-admin all-tenant access (requires a separate authorization and audit design);
- background jobs;
- hard delete (an explicitly authorized purge path);
- GDPR workflows and compliance claims;
- PostgreSQL RLS / SQL Server security policies (separate defense-in-depth);
- migrations, compiled queries, `ExecuteUpdate` / `ExecuteDelete`;
- Minimal APIs, repositories, Testcontainers, benchmarks.

Audit columns (`CreatedAt`/`CreatedBy`/`UpdatedAt`/`UpdatedBy`/`DeletedAt`/`DeletedBy`) are
current-row metadata, **not** an immutable historical audit log. SQLite in-memory is a
lightweight relational test double, not production-provider equivalence. This sample makes no
compliance claim.

## Verification table

| Target framework | .NET 10 (net10.0) |
| --- | --- |
| EF Core package | Microsoft.EntityFrameworkCore.Sqlite 10.0.11 (only direct app package) |
| Test packages | xunit.v3 3.2.2, xunit.runner.visualstudio 3.1.5, Microsoft.NET.Test.Sdk 18.8.1 |
| External services | none (in-memory SQLite) |
| Last reviewed | 2026-08-17 |

## License

MIT &mdash; see the repository root [`LICENSE`](../../LICENSE).
