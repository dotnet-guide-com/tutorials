# Architecture Testing in .NET &mdash; Minimal Layer Rule

A minimal .NET 10 architecture-testing sample demonstrating how a dedicated
test project inspects compiled assemblies and fails the build when a forbidden
dependency appears.

## Full tutorial

[Architecture Testing in .NET: Enforce Layer and Module Boundaries with NetArchTest and ArchUnitNET](https://www.dotnet-guide.com/tutorials/software-architecture/architecture-testing-dotnet/)

## What this sample demonstrates

* a layered .NET 10 solution with Domain, Application, and Infrastructure projects;
* an architecture-test project using **NetArchTest.eNhancedEdition**;
* typed assembly markers that give the test project a reliable way to locate each assembly;
* one forbidden-dependency rule that prevents Domain from depending on outer layers;
* selector non-empty protection so the rule cannot silently pass when no types are found;
* readable failure output naming the protected rule and the offending types;
* normal `dotnet test` execution through xUnit v3.

## Architecture

```text
Infrastructure ──► Application ──► Domain

Allowed direction: toward Domain (outer → inner)
Rejected direction: Domain toward outer layers
```

```text
ArchitectureTests
     │
     ├── inspects Domain
     ├── inspects Application
     └── inspects Infrastructure
```

## File structure

```text
architecture-testing-dotnet/
├── ArchitectureGuardMinimal.slnx
├── README.md
├── src/
│   ├── ArchitectureGuard.Domain/
│   │   ├── ArchitectureGuard.Domain.csproj
│   │   ├── DomainAssemblyMarker.cs
│   │   └── Order.cs
│   ├── ArchitectureGuard.Application/
│   │   ├── ArchitectureGuard.Application.csproj
│   │   ├── ApplicationAssemblyMarker.cs
│   │   └── GetOrderSummary.cs
│   └── ArchitectureGuard.Infrastructure/
│       ├── ArchitectureGuard.Infrastructure.csproj
│       ├── InfrastructureAssemblyMarker.cs
│       └── InMemoryOrderRepository.cs
└── tests/
    └── ArchitectureGuard.ArchitectureTests/
        ├── ArchitectureGuard.ArchitectureTests.csproj
        └── LayerRules.cs
```

## Prerequisites

* .NET 10 SDK

No Docker, Podman, databases, API keys, or cloud credentials are required.

## Run the sample

Open a terminal in this sample folder and run:

```powershell
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
```

Expected output:

```text
Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1
```

## Prove the rule works

To verify that the architecture test genuinely enforces layer boundaries,
temporarily add a forbidden dependency:

1. Add an Infrastructure project reference to Domain:
   ```powershell
   dotnet add src/ArchitectureGuard.Domain/ArchitectureGuard.Domain.csproj reference src/ArchitectureGuard.Infrastructure/ArchitectureGuard.Infrastructure.csproj
   ```

2. Edit `src/ArchitectureGuard.Domain/Order.cs` to reference an outer type:
   ```csharp
   // Temporary violation — add this using:
   using ArchitectureGuard.Infrastructure;
   
   namespace ArchitectureGuard.Domain;
   
   public sealed record Order(Guid Id, decimal Total)
   {
       // Add a temporary field that references the outer layer:
       private static readonly InfrastructureAssemblyMarker _marker = new();
   }
   ```

3. Run the tests again:
   ```powershell
   dotnet build --configuration Release --no-restore
   dotnet test --configuration Release --no-build
   ```

   The architecture test now fails with a message like:
   ```text
   Architecture rule failed: Domain must remain independent of outer layers.
    - ArchitectureGuard.Domain.Order
      [dependency explanation]
   ```

4. Remove the temporary violation:
   ```powershell
   dotnet remove src/ArchitectureGuard.Domain/ArchitectureGuard.Domain.csproj reference src/ArchitectureGuard.Infrastructure/ArchitectureGuard.Infrastructure.csproj
   ```
   Then revert `Order.cs` to its original content.

5. Rebuild and confirm the test passes again.

Do **not** commit the violation.

## Project references

```text
ArchitectureGuard.Application
└── references ArchitectureGuard.Domain

ArchitectureGuard.Infrastructure
├── references ArchitectureGuard.Application
└── references ArchitectureGuard.Domain

ArchitectureGuard.ArchitectureTests
├── references ArchitectureGuard.Domain
├── references ArchitectureGuard.Application
└── references ArchitectureGuard.Infrastructure
```

Domain references nothing.

## Important boundary

This repository sample intentionally does **not** reproduce the complete tutorial.
The full DOTNET GUIDE tutorial additionally contains:

* ArchUnitNET;
* Roslyn analyzer projects;
* `Microsoft.CodeAnalysis.CSharp`;
* custom analyzer packaging;
* analyzer unit tests;
* modular-monolith slicing (Orders and Billing modules);
* contracts assemblies;
* reflection-based endpoint authorization checks;
* naming and sealing convention suites;
* value-object immutability rules;
* legacy violation baselines;
* multiple architecture-test frameworks;
* a web API project;
* controllers or Minimal API endpoints;
* EF Core;
* databases;
* authentication;
* containers;
* cloud services;
* production deployment;
* extensive performance optimization;
* complete production-readiness policy.

## Verification

| Item | Detail |
| --- | --- |
| Target framework | .NET 10 (`net10.0`) |
| SDK | .NET SDK 10.0.302 |
| Architecture library | `NetArchTest.eNhancedEdition` 1.4.5 |
| Test framework | `xunit.v3` 3.2.2 |
| Test runner | `xunit.runner.visualstudio` 3.1.5 |
| Test SDK | `Microsoft.NET.Test.Sdk` 17.14.1 |
| External services required | None |
| Containers required | None |
| API keys required | None |
| Release build | Verified |
| Architecture test | Verified |
| Last reviewed | 2026-07-27 |

## License

Repository-owned sample code is available under the [MIT License](../../LICENSE).

Third-party NetArchTest and xUnit packages remain subject to their own licenses.