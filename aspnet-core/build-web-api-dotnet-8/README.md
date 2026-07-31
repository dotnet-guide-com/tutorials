# ASP.NET Core Todo API &mdash; Minimal Fundamentals Sample

A focused .NET 10 companion showing how core ASP.NET Core Web API building
blocks work together in a small Todo API.

## Full tutorial

[ASP.NET Core Fundamentals: Build Web APIs on .NET 8](https://www.dotnet-guide.com/tutorials/aspnet-core/build-web-api-dotnet-8/)

## Framework note

The tutorial explains ASP.NET Core 8.

This companion targets .NET 10 so it can use the DOTNET GUIDE repository's
current SDK and CI toolchain. The sample demonstrates the same foundational
routing, dependency-injection, request-binding, middleware, CRUD, and HTTP-result
patterns.

## What this sample demonstrates

- Minimal API route groups;
- route parameters and constraints;
- JSON request-body binding;
- dependency injection into route handlers;
- an in-memory repository;
- GET, POST, PUT, and DELETE operations;
- validation responses;
- `200`, `201`, `204`, `400`, and `404` semantics;
- a `Location` header after resource creation;
- a small request-ID middleware;
- integration testing with `WebApplicationFactory<Program>`.

## Important boundary

This repository sample intentionally does not reproduce the complete tutorial.

The full DOTNET GUIDE tutorial also covers:

- controllers and MVC patterns;
- configuration sources;
- EF Core and SQLite;
- migrations;
- JWT authentication and authorization;
- caching;
- rate limiting;
- health-check strategy;
- performance guidance;
- migration from ASP.NET Core 7;
- deployment options.

The separate repository sample
[`aspnet-core/api-security-in-practice`](../api-security-in-practice/)
contains a focused security example.

## Prerequisite

- .NET 10 SDK

Check:

```powershell
dotnet --version
```

## Restore, build, and test

```powershell
dotnet restore .\TodoApiMinimal.slnx

dotnet build `
  .\TodoApiMinimal.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\TodoApiMinimal.slnx `
  --configuration Release `
  --no-build
```

## Run

```powershell
dotnet run `
  --project .\src\TodoApiMinimal\TodoApiMinimal.csproj `
  --urls http://localhost:5128
```

## Endpoints

```text
GET    /
GET    /health
GET    /api/todos
GET    /api/todos/{id}
POST   /api/todos
PUT    /api/todos/{id}
DELETE /api/todos/{id}
```

## Example request

```powershell
$body = @{
  title = "Review HTTP status codes"
} | ConvertTo-Json

Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5128/api/todos" `
  -ContentType "application/json" `
  -Body $body
```

## Persistence limitation

The repository is stored only in application memory.

Restarting the process resets the sample to its two seeded Todo items.

No production persistence is implied.

## Project structure

```text
TodoApiMinimal.slnx
README.md
src/
└── TodoApiMinimal/
    ├── TodoApiMinimal.csproj
    └── Program.cs
tests/
└── TodoApiMinimal.Tests/
    ├── TodoApiMinimal.Tests.csproj
    └── TodoApiTests.cs
```

## Deliberately omitted

- controllers;
- EF Core;
- SQLite;
- migrations;
- authentication;
- authorization;
- CORS;
- caching;
- rate limiting;
- Swagger or OpenAPI UI;
- Docker;
- cloud deployment;
- production storage.

These are covered by the full tutorial or another focused companion sample.

## Verification

- Companion target framework: .NET 10
- Tutorial framework: .NET 8
- External services required: none
- API keys required: none
- Database required: none
- Expected integration tests: 7
- Last reviewed: 2026-07-31

This sample is educational and should be reviewed before production use.