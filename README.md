# dotnet-guide.com - Tutorial Code Samples

Runnable .NET code samples that accompany tutorials published on
[dotnet-guide.com](https://www.dotnet-guide.com/).

Each sample folder contains a focused implementation of one tutorial topic. The full explanation, design reasoning, production considerations, and step-by-step walkthrough remain on the corresponding DOTNET GUIDE tutorial page.

## Available samples

| Sample | What it demonstrates | Full tutorial |
| --- | --- | --- |
| [`dotnet-ai/provider-agnostic-chat-gateway`](dotnet-ai/provider-agnostic-chat-gateway/) | One HTTP chat endpoint using Ollama and optional OpenAI providers through `Microsoft.Extensions.AI.IChatClient` | [Build a switchable multi-provider AI gateway with IChatClient](https://www.dotnet-guide.com/tutorials/dotnet-ai/provider-agnostic-chat-gateway/) |
| [`dotnet-ai/hybrid-search-ef-core-pgvector`](dotnet-ai/hybrid-search-ef-core-pgvector/) | Minimal Reciprocal Rank Fusion demo combining pre-ranked keyword and vector results | [Hybrid Search in .NET with EF Core 10 and pgvector](https://www.dotnet-guide.com/tutorials/dotnet-ai/hybrid-search-ef-core-pgvector/) |
| [`dotnet-aspire/orchestrate-distributed-system`](dotnet-aspire/orchestrate-distributed-system/) | Minimal Aspire AppHost coordinating a web project and API with service discovery and startup ordering | [Aspire in .NET: Orchestrate, Run, and Deploy a Distributed System from One App Host](https://www.dotnet-guide.com/tutorials/dotnet-aspire/orchestrate-distributed-system/) |
| [`software-architecture/architecture-testing-dotnet`](software-architecture/architecture-testing-dotnet/) | Minimal NetArchTest.eNhancedEdition rule that prevents Domain from depending on outer layers | [Architecture Testing in .NET: Enforce Layer and Module Boundaries with NetArchTest and ArchUnitNET](https://www.dotnet-guide.com/tutorials/software-architecture/architecture-testing-dotnet/) |
| [`distributed-systems/transactional-outbox-ef-core`](distributed-systems/transactional-outbox-ef-core/) | Minimal EF Core and SQLite demonstration that saves business state and an outbox message atomically, then publishes it through a one-shot relay | [Transactional Outbox Pattern in .NET with EF Core (.NET 10): Fix the Dual-Write Problem](https://www.dotnet-guide.com/tutorials/distributed-systems/transactional-outbox-ef-core/) |
| [`aspnet-core/api-security-in-practice`](aspnet-core/api-security-in-practice/) | Minimal JWT bearer authentication, note ownership enforcement, and rate limiting for an ASP.NET Core API | [ASP.NET Core 8 API Security: JWT Authentication, CSRF Protection & Rate Limiting](https://www.dotnet-guide.com/tutorials/aspnet-core/api-security-in-practice/) |
| [`aspnet-core/passkey-first-identity`](aspnet-core/passkey-first-identity/) | Minimal .NET 10 Identity sample for passkey enrollment, username-first passkey sign-in, antiforgery protection, and stored credential listing | [Passkeys in ASP.NET Core Identity (.NET 10): Build a Passwordless-First Web App with WebAuthn](https://www.dotnet-guide.com/tutorials/aspnet-core/passkey-first-identity/) |
| [`aspnet-core/build-web-api-dotnet-8`](aspnet-core/build-web-api-dotnet-8/) | Minimal .NET 10 Todo API demonstrating route groups, dependency injection, CRUD status codes, validation, middleware, and integration testing | [ASP.NET Core Fundamentals: Build Web APIs on .NET 8](https://www.dotnet-guide.com/tutorials/aspnet-core/build-web-api-dotnet-8/) |
| [`aspnet-core/caching-outputcache-redis-invalidation`](aspnet-core/caching-outputcache-redis-invalidation/) | Minimal .NET 10 Catalog API demonstrating Output Cache policies, query and route variation, tag eviction, write-path invalidation, and integration testing | [ASP.NET Core Caching: Output Cache, Redis & Invalidation Strategies That Actually Work](https://www.dotnet-guide.com/tutorials/aspnet-core/caching-outputcache-redis-invalidation/) |

## Companion articles
- [Common Microsoft.Extensions.AI mistakes](https://www.dotnet-guide.com/articles/dotnet-ai/microsoft-extensions-ai-common-mistakes/)
- [Vector search in .NET: Common mistakes](https://www.dotnet-guide.com/articles/dotnet-ai/vector-search-common-mistakes/)
- [NetArchTest, ArchUnitNET, and Roslyn analyzers](https://www.dotnet-guide.com/articles/software-architecture/netarchtest-archunitnet-roslyn-analyzers/)
- [Transactional outbox relay: Common mistakes](https://www.dotnet-guide.com/articles/distributed-systems/outbox-relay-common-mistakes/)
- [ASP.NET Core API security checklist](https://www.dotnet-guide.com/articles/aspnet-core-api-security-checklist/)

## Download and run

1. Click the green **Code** button.
2. Choose **Download ZIP**.
3. Extract the downloaded archive.
4. Open PowerShell in the required sample folder.
5. Follow that folder's `README.md`.

### Provider-agnostic chat gateway

```powershell
cd dotnet-ai\provider-agnostic-chat-gateway
dotnet restore
dotnet run --urls http://localhost:5123
````

### Hybrid search RRF sample

```powershell
cd dotnet-ai\hybrid-search-ef-core-pgvector
dotnet restore
dotnet run
```

## Repository structure

```text
tutorials/
|-- dotnet-ai/
|   |-- provider-agnostic-chat-gateway/
|   |   |-- ChatGatewayMinimal.csproj
|   |   |-- Program.cs
|   |   `-- README.md
|   `-- hybrid-search-ef-core-pgvector/
|       |-- HybridSearchMinimal.csproj
|       |-- Program.cs
|       `-- README.md
|-- dotnet-aspire/
|   `-- orchestrate-distributed-system/
|       |-- AspireOrchestrationMinimal.slnx
|       |-- README.md
|       |-- AspireOrchestrationMinimal.AppHost/
|       |   |-- AppHost.cs
|       |   `-- AspireOrchestrationMinimal.AppHost.csproj
|       |-- AspireOrchestrationMinimal.Api/
|       |   |-- Program.cs
|       |   `-- AspireOrchestrationMinimal.Api.csproj
|       `-- AspireOrchestrationMinimal.Web/
|           |-- Program.cs
|           `-- AspireOrchestrationMinimal.Web.csproj
|-- software-architecture/
|   `-- architecture-testing-dotnet/
|       |-- ArchitectureGuardMinimal.slnx
|       |-- README.md
|       |-- src/
|       |   |-- ArchitectureGuard.Domain/
|       |   |   |-- ArchitectureGuard.Domain.csproj
|       |   |   |-- DomainAssemblyMarker.cs
|       |   |   `-- Order.cs
|       |   |-- ArchitectureGuard.Application/
|       |   |   |-- ArchitectureGuard.Application.csproj
|       |   |   |-- ApplicationAssemblyMarker.cs
|       |   |   `-- GetOrderSummary.cs
|       |   `-- ArchitectureGuard.Infrastructure/
|       |       |-- ArchitectureGuard.Infrastructure.csproj
|       |       |-- InfrastructureAssemblyMarker.cs
|       |       `-- InMemoryOrderRepository.cs
|       `-- tests/
|           `-- ArchitectureGuard.ArchitectureTests/
|               |-- ArchitectureGuard.ArchitectureTests.csproj
|               `-- LayerRules.cs
|-- distributed-systems/
|   `-- transactional-outbox-ef-core/
|       |-- TransactionalOutboxMinimal.slnx
|       |-- README.md
|       |-- src/
|       |   `-- TransactionalOutboxMinimal/
|       |       |-- TransactionalOutboxMinimal.csproj
|       |       |-- Program.cs
|       |       |-- Data/
|       |       |   `-- OrdersDbContext.cs
|       |       |-- Domain/
|       |       |   `-- Order.cs
|       |       |-- Messaging/
|       |       |   |-- OrderPlaced.cs
|       |       |   |-- OutboxMessage.cs
|       |       |   |-- IPublisher.cs
|       |       |   |-- InMemoryPublisher.cs
|       |       |   `-- OutboxRelay.cs
|       |       `-- Services/
|       |           `-- OrderService.cs
|       `-- tests/
|           `-- TransactionalOutboxMinimal.Tests/
|               |-- TransactionalOutboxMinimal.Tests.csproj
|               `-- OutboxFlowTests.cs
|-- aspnet-core/
|   |-- api-security-in-practice/
|   |   |-- ApiSecurityMinimal.slnx
|   |   |-- README.md
|   |   |-- src/
|   |   |   `-- ApiSecurityMinimal/
|   |   |       |-- ApiSecurityMinimal.csproj
|   |   |       |-- Program.cs
|   |   |       |-- Models.cs
|   |   |       |-- DemoStore.cs
|   |   |       |-- JwtTokenService.cs
|   |   |       `-- appsettings.json
|   |   `-- tests/
|   |       `-- ApiSecurityMinimal.Tests/
|   |           |-- ApiSecurityMinimal.Tests.csproj
|   |           `-- ApiSecurityTests.cs
|   |-- passkey-first-identity/
|   |   |-- PasskeyIdentityMinimal.slnx
|   |   |-- README.md
|   |   |-- src/
|   |   |   `-- PasskeyIdentityMinimal/
|   |   |       |-- PasskeyIdentityMinimal.csproj
|   |   |       |-- Program.cs
|   |   |       |-- appsettings.json
|   |   |       |-- Data/
|   |   |       |   |-- ApplicationDbContext.cs
|   |   |       |   `-- ApplicationUser.cs
|   |   |       |-- Endpoints/
|   |   |       |   |-- AntiforgeryEndpointExtensions.cs
|   |   |       |   `-- PasskeyEndpoints.cs
|   |   |       `-- wwwroot/
|   |   |           |-- index.html
|   |   |           `-- passkeys.js
|   |   `-- tests/
|   |       `-- PasskeyIdentityMinimal.Tests/
|   |           |-- PasskeyIdentityMinimal.Tests.csproj
|   |           |-- PasskeyEndpointTests.cs
|   |           `-- PasskeyIdentityFactory.cs
|   |-- build-web-api-dotnet-8/
|   |   |-- TodoApiMinimal.slnx
|   |   |-- README.md
|   |   |-- src/
|   |   |   `-- TodoApiMinimal/
|   |   |       |-- TodoApiMinimal.csproj
|   |   |       `-- Program.cs
|   |   `-- tests/
|   |       `-- TodoApiMinimal.Tests/
|   |           |-- TodoApiMinimal.Tests.csproj
|   |           `-- TodoApiTests.cs
|   `-- caching-outputcache-redis-invalidation/
|       |-- OutputCacheCatalogMinimal.slnx
|       |-- README.md
|       |-- src/
|       |   `-- OutputCacheCatalogMinimal/
|       |       |-- OutputCacheCatalogMinimal.csproj
|       |       `-- Program.cs
|       `-- tests/
|           `-- OutputCacheCatalogMinimal.Tests/
|               |-- OutputCacheCatalogMinimal.Tests.csproj
|               `-- OutputCacheTests.cs
|-- .github/
|   `-- workflows/
|       `-- build-samples.yml
|-- .gitignore
|-- LICENSE
`-- README.md
```

## Repository boundaries

This repository contains educational code samples only.

It does not contain:

* the DOTNET GUIDE website source code
* production credentials
* analytics configuration
* private deployment paths
* API keys
* complete production applications

The sample folders are intentionally smaller than their corresponding tutorials. Advanced setup, design discussion, database configuration, production hardening, and extended implementation guidance remain on the DOTNET GUIDE website.

## Versions and verification

Each sample identifies:

* its target .NET version
* its required NuGet package versions, when applicable
* its local model, database, or service requirements
* its last-reviewed date
* the commands needed to restore, build, and run it

Some lightweight samples may have no external package dependencies or service requirements.

Libraries and platforms related to AI, authentication, Aspire, distributed systems, databases, and vector search may change quickly. Check the sample's review date and current official documentation before adapting it to a newer SDK or package version.

## Independence

dotnet-guide.com is an independent educational website. It is not affiliated with, endorsed by, or connected to Microsoft Corporation.

Microsoft, .NET, C#, ASP.NET Core, Azure, and related names may be trademarks of Microsoft Corporation. Other product names belong to their respective owners.

## Corrections

Found a compile error or outdated API?

Open an issue and include:

* the sample path
* your .NET SDK version
* the command you ran
* the complete error message
* your operating system

Small and focused pull requests are also welcome.

## License

Repository-owned sample code is available under the [MIT License](LICENSE).

Third-party libraries remain subject to their own licenses.

## Website links

* [DOTNET GUIDE home](https://www.dotnet-guide.com/)
* [Tutorials](https://www.dotnet-guide.com/tutorials/)
* [Articles](https://www.dotnet-guide.com/articles/)
* [Editorial policy](https://www.dotnet-guide.com/editorial/)
* [About DOTNET GUIDE](https://www.dotnet-guide.com/about/)
