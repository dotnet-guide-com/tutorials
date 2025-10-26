# ASP.NET Core – sample code for the tutorial

This folder contains two runnable Web API projects used in the guide **“ASP.NET Core Fundamentals: Build Web APIs with .NET 8.”**

## Prerequisites
- .NET 8 SDK installed (`dotnet --version` should show `8.x`)
- Any editor (VS 2022 17.8+, VS Code + C# extension, or Rider)

---

## 1) TodoApiBasic (minimal API)

**What it shows**
- Minimal APIs
- CRUD endpoints with in-memory list
- Swagger/OpenAPI

**Run**
```bash
cd tutorials/aspnet-core/TodoApiBasic
dotnet restore
dotnet run
