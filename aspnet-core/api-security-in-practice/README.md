# ASP.NET Core API Security &mdash; Minimal JWT and Rate Limiting Sample

Full tutorial: [ASP.NET Core 8 API Security: JWT Authentication, CSRF Protection & Rate Limiting](https://www.dotnet-guide.com/tutorials/aspnet-core/api-security-in-practice/)

## What this sample demonstrates

- .NET 10 ASP.NET Core Minimal API
- JWT bearer authentication with issuer, audience, lifetime, signing-key, and algorithm validation
- Short-lived tokens (30 minutes)
- Development-only login endpoint (`POST /auth/token`)
- One seeded demo user
- One protected Notes resource with ownership checks
- 404 for another user's hidden note (anti-enumeration)
- Login rate limiting (5 requests/minute)
- Per-user API rate limiting (20 requests/minute)
- JSON 401 and 429 responses following RFC-style
- Integration tests using `WebApplicationFactory`

## Architecture

```
Client
  |
  |-- POST /auth/token
  |         |
  |         +-- signed JWT
  |
  +-- Authorization: Bearer <token>
            |
            v
       Protected Notes API
            |
            |-- ownership check
            +-- per-user rate limit
```

## File structure

```
aspnet-core/
+-- api-security-in-practice/
    |-- ApiSecurityMinimal.slnx
    |-- README.md
    |-- src/
    |   +-- ApiSecurityMinimal/
    |       |-- ApiSecurityMinimal.csproj
    |       |-- Program.cs
    |       |-- Models.cs
    |       |-- DemoStore.cs
    |       |-- JwtTokenService.cs
    |       +-- appsettings.json
    +-- tests/
        +-- ApiSecurityMinimal.Tests/
            |-- ApiSecurityMinimal.Tests.csproj
            +-- ApiSecurityTests.cs
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

No database, Docker, or external service is required.

## Demo credentials

| Field | Value |
| --- | --- |
| Email | `demo@example.com` |
| Password | `DemoPass123!` |

These are development-only credentials. Production systems must use ASP.NET Core Identity or a proper password hasher.

## Setup

Set a JWT signing key using a method that does not commit the key to source control.

### Environment variable (recommended for CI / quick testing)

```powershell
$env:Jwt__Key = "<development-key-at-least-32-bytes>"
```

### User secrets (recommended for local development)

```powershell
dotnet user-secrets init --project src/ApiSecurityMinimal
dotnet user-secrets set "Jwt:Key" "<development-key-at-least-32-bytes>" --project src/ApiSecurityMinimal
```

## Run

```powershell
cd aspnet-core\api-security-in-practice

dotnet restore
dotnet build --configuration Release --no-restore

# Run tests
dotnet test --configuration Release --no-build

# Run the API
$env:Jwt__Key = "<development-key-at-least-32-bytes>"
dotnet run --project src\ApiSecurityMinimal --configuration Release --no-build
```

### Manual API test

```powershell
# Login
curl.exe -X POST "http://localhost:<port>/auth/token" `
  -H "Content-Type: application/json" `
  -d "{\"email\":\"demo@example.com\",\"password\":\"DemoPass123!\"}"

# Use the returned token for protected endpoints
curl.exe "http://localhost:<port>/notes" `
  -H "Authorization: Bearer <token>"
```

The actual local port is shown by `dotnet run` and varies per run.

## Verify

| Request | Expected result |
| --- | --- |
| `GET /health` (anonymous) | 200 `{"status":"healthy"}` |
| `POST /auth/token` with valid credentials | 200 + JWT |
| `POST /auth/token` with invalid credentials | 401 |
| `GET /notes` without token | 401 |
| `GET /notes` with valid token | 200 + notes (initially empty) |
| `DELETE /notes/{id}` owned by another user | 404 |
| 6th login attempt within a minute | 429 + `Retry-After` |

## Important boundary

This is an **educational JWT-only sample**, not a complete identity system. It intentionally omits:

- Cookie authentication
- CSRF tokens and antiforgery middleware
- CORS configuration
- HSTS and browser security headers
- ASP.NET Core Identity
- Password hashing (plain-text demo password)
- Refresh tokens and token revocation
- Persistent database
- Distributed rate limiting
- OAuth 2.0 / OpenID Connect
- Production deployment

The full tutorial remains the authoritative source for all of the above.

## Version note

The tutorial is written around **ASP.NET Core 8**. The companion sample targets **.NET 10** while using the same core authentication, authorization, and rate-limiting concepts.

## Security disclosures

- The demo password is intentionally simple and local-only
- Production systems should use ASP.NET Core Identity or an external identity provider
- Production signing keys belong in a secret manager
- Access tokens should be short-lived
- Refresh-token rotation and revocation are not implemented
- Cookie authentication and CSRF protection are intentionally excluded
- CORS and browser security headers are intentionally excluded
- Rate limiting is in-process and not distributed across instances
- The in-memory store resets on restart

## Verification details

| Item | Value |
| --- | --- |
| Target framework | `net10.0` |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.10 |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.10 |
| `xunit.v3` | 3.2.2 |
| `xunit.runner.visualstudio` | 3.1.5 |
| `Microsoft.NET.Test.Sdk` | 17.14.1 |
| External services required | None |
| Containers required | None |
| API keys required | None |
| Runtime secret required | Development JWT signing key |
| Last reviewed | July 28, 2026 |
| Release build | Verified |
| Tests | Verified (6/6 passing) |

## License

Sample code in this folder is available under the [MIT License](../LICENSE).