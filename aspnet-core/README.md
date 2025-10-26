# ASP.NET Core – sample code for the tutorial

This folder contains two runnable Web API projects used in the guide  
**“ASP.NET Core Fundamentals: Build Web APIs with .NET 8.”**

- ✅ **TodoApiBasic** – minimal API, in-memory list, Swagger/OpenAPI
- 🔐 **TodoApiEfJwt** – minimal API + EF Core (SQLite) + JWT auth

---

## Prerequisites

- **.NET 8 SDK** installed (`dotnet --version` should start with `8.`)
- Any editor (**VS 2022 17.8+**, **VS Code** with C# extension, or **Rider**)
- Optional: **curl** for quick requests from the terminal

> If HTTPS endpoints fail locally, trust dev certs:
>
> ```bash
> dotnet dev-certs https --trust
> ```

---

## 1) Run **TodoApiBasic** (minimal API)

What it shows:

- Minimal APIs
- CRUD endpoints with an in-memory list
- Built-in **Swagger** UI (OpenAPI)

### Run

```bash
cd tutorials/aspnet-core/TodoApiBasic
dotnet restore
dotnet run

Open Swagger UI:

https://localhost:5001/swagger
http://localhost:5000/swagger

2) Run TodoApiEfJwt (EF Core + JWT)

What it shows:

Minimal API + EF Core (SQLite)
Basic JWT authentication with a simple token-issuing endpoint
Protected CRUD endpoints

⚠️ The JWT setup here is intentionally simple for learning.
For production, use secure secret storage, HTTPS, proper token lifetimes/refresh, etc.

First-time setup

1. Install EF Core CLI (if you don’t have it yet):
dotnet tool install -g dotnet-ef
# or update:
# dotnet tool update -g dotnet-ef

2. Restore & create the database:
cd tutorials/aspnet-core/TodoApiEfJwt
dotnet restore

# create initial migration & SQLite DB (todos.db)
dotnet ef migrations add InitialCreate
dotnet ef database update

3. Run the API:

dotnet run

You should see the URLs (HTTPS/HTTP). Open /swagger:

https://localhost:5001/swagger
http://localhost:5000/swagger

Get a JWT

Use the Swagger UI (POST /auth/token) with this body:
{ "username": "demo" }

Copy the access_token value from the response.
You can also use curl:
curl -k -X POST "https://localhost:5001/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"username":"demo"}'

Call protected endpoints

In Swagger, click Authorize, paste Bearer YOUR_TOKEN_HERE, then try:

GET /api/todos
POST /api/todos
PUT /api/todos/{id}
DELETE /api/todos/{id}

With curl (macOS/Linux):

export TOKEN=PASTE_JWT_HERE

# Create
curl -k -X POST "https://localhost:5001/api/todos" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Write docs","isComplete":false}'

# List
curl -k "https://localhost:5001/api/todos" \
  -H "Authorization: Bearer $TOKEN"

With curl (Windows PowerShell):

$env:TOKEN = "PASTE_JWT_HERE"

# Create
curl -k -Method POST "https://localhost:5001/api/todos" `
  -H "Authorization: Bearer $($env:TOKEN)" `
  -H "Content-Type: application/json" `
  -Body '{"title":"Write docs","isComplete":false}'

# List
curl -k "https://localhost:5001/api/todos" `
  -H "Authorization: Bearer $($env:TOKEN)"

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

Swagger isn’t loading on HTTPS
Run dotnet dev-certs https --trust (accept the prompt), or open the HTTP URL.

dotnet-ef not found
Run dotnet tool install -g dotnet-ef (or update), then restart the terminal.

401 Unauthorized on protected endpoints
Click Authorize in Swagger and paste Bearer YOUR_TOKEN. Tokens expire—get a new one if needed.

Database locked / errors
Stop the app, delete todos.db, then run:
dotnet ef database update

Issuer/Audience mismatch
Confirm the values in appsettings.json (Jwt:Issuer, Jwt:Audience) match token creation config in Program.cs.

Code targets .NET 8.
Examples are deliberately small and focused for learning.
For production, harden authentication, secrets handling, error handling, logging, and deployment settings.

Happy building! 🚀

