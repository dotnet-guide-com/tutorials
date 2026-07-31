# Passkey-First Identity in ASP.NET Core &mdash; Minimal .NET 10 Sample

Full tutorial: [Passkeys in ASP.NET Core Identity (.NET 10): Build a Passwordless-First Web App with WebAuthn](https://www.dotnet-guide.com/tutorials/aspnet-core/passkey-first-identity/)

## What this sample demonstrates

- .NET 10 ASP.NET Core Identity with schema version 3
- SQLite database with `EnsureCreated` schema bootstrapping
- Development-only bootstrap password login
- Passkey creation options via `SignInManager.MakePasskeyCreationOptionsAsync`
- Browser WebAuthn `navigator.credentials.create()` with native JSON serialization
- Attestation verification via `SignInManager.PerformPasskeyAttestationAsync`
- Stored passkey persistence via `UserManager.AddOrUpdatePasskeyAsync`
- Username-first passkey request options via `SignInManager.MakePasskeyRequestOptionsAsync`
- Passkey sign-in via `SignInManager.PasskeySignInAsync`
- Passkey listing via `UserManager.GetPasskeysAsync`
- Antiforgery validation on every state-changing passkey endpoint
- Server-side integration tests using `WebApplicationFactory`

## Architecture diagram

```
Bootstrap login
      │
      ▼
Identity cookie
      │
      ├── creation options
      ├── browser authenticator
      └── verified passkey storage

Later sign-in
      │
      ├── request options
      ├── signed assertion
      └── Identity cookie
```

## File structure

```
aspnet-core/
└── passkey-first-identity/
    ├── PasskeyIdentityMinimal.slnx
    ├── README.md
    ├── src/
    │   └── PasskeyIdentityMinimal/
    │       ├── PasskeyIdentityMinimal.csproj
    │       ├── Program.cs
    │       ├── appsettings.json
    │       ├── Data/
    │       │   ├── ApplicationDbContext.cs
    │       │   └── ApplicationUser.cs
    │       ├── Endpoints/
    │       │   ├── AntiforgeryEndpointExtensions.cs
    │       │   └── PasskeyEndpoints.cs
    │       └── wwwroot/
    │           ├── index.html
    │           └── passkeys.js
    └── tests/
        └── PasskeyIdentityMinimal.Tests/
            ├── PasskeyIdentityMinimal.Tests.csproj
            ├── PasskeyEndpointTests.cs
            └── PasskeyIdentityFactory.cs
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A browser that supports WebAuthn (Chrome, Edge, Firefox, Safari)
- HTTPS is required for WebAuthn in production; localhost is exempt

## Run instructions

```powershell
cd aspnet-core\passkey-first-identity
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet run --project src\PasskeyIdentityMinimal --configuration Release --no-build
```

On Linux/macOS:

```bash
cd aspnet-core/passkey-first-identity
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/PasskeyIdentityMinimal --configuration Release --no-build
```

The application starts on `https://localhost:5001` and `http://localhost:5000`.

## Development credentials

The following are sample development credentials used for local development and testing only.

| Field    | Value            |
|----------|------------------|
| Email    | `demo@example.com` |
| Password | `DemoPass123!`     |

> **Development only:** These sample credentials are intended exclusively for local Development and Testing environments. The bootstrap user exists only to enroll the initial passkey during setup and is never intended for production use. Do not deploy, reuse, or rely on these credentials in any production environment.

## Manual browser test

1. Trust the development certificate:
   ```bash
   dotnet dev-certs https --trust
   ```

2. Start the application in the `Development` environment:
   ```bash
   ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/PasskeyIdentityMinimal
   ```

3. Open `https://localhost:5001` in your browser.

4. Click **Sign in with password** (the credentials are pre-filled).

5. After a successful sign-in, click **Create passkey**.

6. Complete the authenticator prompt (Windows Hello, Touch ID, browser virtual authenticator, or another supported authenticator).

7. Click **List my passkeys** to confirm the passkey appears.

8. Click **Sign out**.

9. Enter `demo@example.com` and click **Sign in with passkey**.

10. Complete the authenticator prompt.

11. Confirm the application shows the authenticated state.

### Using a browser virtual authenticator (Chrome)

1. Open Chrome DevTools (`F12`).
2. Click **More tools** &rarr; **WebAuthn**.
3. Check **Enable virtual authenticator environment**.
4. Click **Add** to create a resident-key-capable authenticator.
5. Complete the passkey registration and sign-in flows described above.

## Important boundary

This sample is deliberately smaller than the full tutorial. It focuses on:

- Bootstrap login
- Passkey enrollment
- Username-first passkey sign-in
- Passkey listing
- Schema and antiforgery correctness

It deliberately omits:

- Conditional UI and username-field autofill
- Username-less (discovery) sign-in
- Passkey rename and deletion
- Final-factor deletion protection
- Password removal
- Verified-email recovery
- Recovery codes
- Email sending
- Attestation-statement policy validation
- Authenticator-model allow or deny lists (AAGUID)
- FIDO Metadata Service integration
- FIDO2 net library
- Organization-managed security keys
- IdentityServer integration
- Selenium, Playwright, or browser automation
- Virtual authenticator orchestration in CI
- MVC or Razor Pages port
- Production reverse-proxy setup
- Distributed rate limiting

These topics remain in the full tutorial.

## Passkey enrollment is not a complete recovery strategy

Losing the only authenticator can permanently lock the user out. Production applications should:

- Encourage users to register at least two passkeys.
- Provide verified email, recovery codes, or another approved factor.
- Protect deletion of the final factor with additional checks.
- Plan for account recovery before users need it.

This lightweight sample does not implement passkey deletion, password removal, or account recovery.

## Built-in attestation-policy limits

ASP.NET Core Identity passkeys are not a complete general-purpose WebAuthn governance stack. This sample does not provide:

- Attestation-statement policy
- Authenticator-model allow or deny lists
- FIDO Metadata Service checks
- Enterprise hardware-key governance

Readers needing those capabilities should use the full tutorial's guidance and assess a full WebAuthn library.

## Verification details

| Item               | Value                                                        |
|--------------------|--------------------------------------------------------------|
| Target framework   | `net10.0`                                                    |
| Package            | Version                                                      |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | `10.0.10`                                                   |
| `Microsoft.EntityFrameworkCore.Sqlite`              | `10.0.10`                                                   |
| `Microsoft.EntityFrameworkCore.Design`              | `10.0.10`                                                   |
| `Microsoft.AspNetCore.Mvc.Testing`                  | `10.0.10`                                                   |
| `Microsoft.NET.Test.Sdk`                            | `17.14.1`                                                    |
| `xunit.v3`                                          | `3.2.2`                                                      |
| `xunit.runner.visualstudio`                         | `3.1.5`                                                      |
| Database            | Temporary SQLite file (`PasskeyIdentityMinimal.db` in the project directory during development; in-memory SQLite during testing) |
| RP ID               | `localhost` (development only; change for production)         |
| External services   | None                                                         |
| Containers          | None                                                         |
| API keys            | None                                                         |
| Hardware authenticator | Optional for manual testing                               |
| Browser virtual authenticator | Supported for manual testing (Chrome DevTools)      |
| Last reviewed       | 2026-07-30                                                   |
| Release build       | Verified                                                     |
| Tests               | Verified (6/6 passing)                                       |
| Browser ceremony    | Requires a WebAuthn-capable browser and authenticator; not verified on this headless build server |

## License

Sample code in this repository is available under the [MIT License](../../LICENSE).
