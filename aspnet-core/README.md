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

Open Swagger UI:

https://localhost:5001/swagger

2) TodoApiEfJwt (EF Core + JWT)

What it shows

Minimal API + EF Core (SQLite)

JWT authentication with a simple token-issuing endpoint

Protected CRUD endpoints

Run (first time)

cd tutorials/aspnet-core/TodoApiEfJwt
dotnet restore
dotnet ef database update   # creates the SQLite DB using the included migration if present, or skip if not needed
dotnet run

Get a JWT:

# POST to /auth/token with a demo username (no password for demo)
# You can use Swagger UI or curl

Then call protected endpoints by clicking Authorize in Swagger and pasting the token.

Note
The JWT setup here is intentionally simple for learning. For production, use secure secret storage, HTTPS, refresh tokens, and proper user management.

Project map

aspnet-core/
  README.md
  TodoApiBasic/
    TodoApiBasic.csproj
    Program.cs
    Todo.cs
  TodoApiEfJwt/
    TodoApiEfJwt.csproj
    Program.cs
    Todo.cs
    TodoDb.cs
    appsettings.json

Troubleshooting

If Swagger isn’t loading on HTTPS, try the HTTP URL shown in the console output.
EF Core tools: dotnet tool install --global dotnet-ef (if dotnet ef isn’t found).
Delete bin/ and obj/ and rerun dotnet restore if builds fail after edits.

