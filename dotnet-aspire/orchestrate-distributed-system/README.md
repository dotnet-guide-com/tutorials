# Aspire AppHost Orchestration &mdash; Minimal Sample

A minimal .NET 10 Aspire sample demonstrating how one AppHost starts two .NET
projects, connects them with a logical reference, and enables the web project
to call the API through service discovery without a fixed localhost port.

## Full tutorial

[Aspire in .NET: Orchestrate, Run, and Deploy a Distributed System from One App Host](https://www.dotnet-guide.com/tutorials/dotnet-aspire/orchestrate-distributed-system/)

## What this sample demonstrates

* one Aspire AppHost;
* one API project;
* one web project;
* service discovery through `https+http://api`;
* project references and `WithReference(api)`;
* startup ordering with `WaitFor(api)`;
* an externally reachable web endpoint through `WithExternalHttpEndpoints`;
* dashboard visibility of both resources;
* web-to-API communication resolved by the AppHost.

## Architecture

```text
AppHost
├── api  ── GET /message
└── web  ── GET /  (calls api through service discovery)
     │
     └── WithReference(api)
     └── WaitFor(api)
```

## File structure

```text
orchestrate-distributed-system/
├── AspireOrchestrationMinimal.slnx
├── README.md
├── AspireOrchestrationMinimal.AppHost/
│   ├── AppHost.cs
│   └── AspireOrchestrationMinimal.AppHost.csproj
├── AspireOrchestrationMinimal.Api/
│   ├── Program.cs
│   └── AspireOrchestrationMinimal.Api.csproj
└── AspireOrchestrationMinimal.Web/
    ├── Program.cs
    └── AspireOrchestrationMinimal.Web.csproj
```

## Prerequisites

* .NET 10 SDK
* HTTPS development certificate trusted (run `dotnet dev-certs https --trust`)

No Docker, Podman, PostgreSQL, Redis, API keys, or cloud credentials are required.

## Run the sample

Open a terminal in this sample folder and run:

```powershell
dotnet restore
dotnet build --configuration Release
dotnet run --project AspireOrchestrationMinimal.AppHost
```

The AppHost starts both projects. The Aspire dashboard opens automatically.

## Verify the sample

### Dashboard

Confirm that the Aspire dashboard shows:

* `api` &mdash; started successfully
* `web` &mdash; started successfully, waited for `api`
* both endpoints are visible

### Web endpoint

Open the web endpoint shown in the dashboard. The page displays:

```text
Aspire orchestration sample

Web resource: running
API response: {"message":"Hello from the Aspire-managed API","service":"api"}
```

### API endpoint

```text
GET /        → AspireOrchestrationMinimal.Api is running.
GET /message → {"message":"Hello from the Aspire-managed API","service":"api"}
```

## Service discovery

The web project uses:

```csharp
client.BaseAddress = new Uri("https+http://api");
```

The resource name `api` matches the AppHost declaration:

```csharp
var api = builder.AddProject<Projects.AspireOrchestrationMinimal_Api>("api");
```

Aspire resolves the logical name at runtime. The web project contains **no**
fixed localhost port such as `http://localhost:5001`.

## `WithReference` versus `WaitFor`

| Method | Purpose |
| --- | --- |
| `WithReference(api)` | Supplies service-discovery and configuration information to the referencing project |
| `WaitFor(api)` | Controls startup ordering — the web project does not start until the API is ready |

They solve different problems. This sample uses both.

## Important boundary

This repository sample intentionally does **not** reproduce the complete tutorial.

The full DOTNET GUIDE tutorial additionally contains:

* PostgreSQL and Redis integration
* ServiceDefaults project
* EF Core and Npgsql
* database migrations
* caching
* custom health-check and readiness packages
* `Aspire.Hosting.Testing`
* integration-test projects
* OpenTelemetry customization
* generative-AI telemetry
* publishing (`aspire publish`)
* deployment (`aspire deploy`)
* Azure deployment guidance
* Kubernetes and Helm
* Docker-oriented publishing
* production secrets, ingress, scaling, and networking
* complete release-management and production-hardening guidance

## Verification

| Item | Detail |
| --- | --- |
| Target framework | .NET 10 (`net10.0`) |
| SDK | .NET SDK 10.0.302 |
| Aspire version | `Aspire.Hosting.AppHost` 13.4.6 |
| Service discovery | `Microsoft.Extensions.ServiceDiscovery` 10.8.0 |
| External services required | None |
| Containers required | None |
| API keys required | None |
| Local restore | Verified |
| Release build | Verified |
| Last reviewed | 2026-07-26 |

## License

Repository-owned sample code is available under the [MIT License](../../LICENSE).

Third-party Aspire and .NET packages remain subject to their own licenses.