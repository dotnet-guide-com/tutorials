# Typed Configuration Precedence & Safe Diagnostics

A focused ASP.NET Core Minimal API companion demonstrating layered
configuration, a project-specific environment-variable prefix, command-line
precedence, strongly typed options, startup validation, User Secrets metadata,
a lightweight feature flag, and an allowlisted Development-only diagnostics
endpoint that never returns the configured API-key value.

## Full tutorial

[.NET 8 Configuration & Secrets Management: Typed Options, User Secrets & Feature Flags](https://www.dotnet-guide.com/tutorials/dotnet-8-essentials/configuration-secrets-environments/)

## Framework note

The full tutorial is written for ASP.NET Core 8.

This companion targets .NET 10 because that is the current DOTNET GUIDE sample
SDK.

## Configuration flow

```text
appsettings.json
  -> appsettings.Development.json
  -> User Secrets in Development
  -> default environment variables
  -> CFGPLAY_ prefixed environment variables
  -> command-line arguments
  -> validated options
```

The sample re-adds the command-line provider after `CFGPLAY_`, so explicit
command-line arguments remain the highest-priority application overrides.

## Base values

```text
TimeoutSeconds = 10
AdvancedSearch = false
```

## Development overrides

```text
TimeoutSeconds = 20
AdvancedSearch = true
```

## Required API key

`App:ApiKey` is intentionally empty in committed JSON.

The app will fail startup until a higher-priority configuration provider
supplies a valid value.

### Local Development with User Secrets

From the application project folder:

```powershell
dotnet user-secrets set `
  "App:ApiKey" `
  "local-demo-key-1234"
```

Secret Manager keeps development values outside the project tree and source
control, but the stored values are not encrypted and Secret Manager is not a
production secret store.

Do not copy or commit `secrets.json`.

## Environment-variable override

The application adds:

```csharp
AddEnvironmentVariables("CFGPLAY_")
```

The prefix is stripped.

For example:

```text
CFGPLAY_App__TimeoutSeconds
```

maps to:

```text
App:TimeoutSeconds
```

Double underscore is the portable hierarchy separator for environment-variable
configuration.

## Command-line precedence

Run:

```powershell
$env:DOTNET_ENVIRONMENT = "Development"
$env:CFGPLAY_App__ApiKey = "local-demo-key-1234"
$env:CFGPLAY_App__TimeoutSeconds = "30"

dotnet run `
  --project .\src\ConfigPrecedenceMinimal\ConfigPrecedenceMinimal.csproj `
  --configuration Release `
  -- `
  --App:TimeoutSeconds=40 `
  --Features:AdvancedSearch=false
```

The final timeout is:

```text
40
```

because the command-line provider is deliberately added last.

## Startup validation

`AppOptions` is validated using:

- DataAnnotations;
- a custom `IValidateOptions<AppOptions>`;
- `ValidateOnStart`.

The app rejects:

- a missing or too-short API key;
- a malformed service URL;
- HTTP when `RequireHttps` is true;
- obvious placeholder API-key values.

## Safe diagnostics

The Development-only endpoint is:

```text
GET /dev/config
```

It returns an explicit allowlist:

```text
environment
serviceBaseUrl
timeoutSeconds
advancedSearch
apiKey
apiKeyConfigured
```

The API-key value is always:

```text
[REDACTED]
```

The sample does not:

- enumerate all of `IConfiguration`;
- expose `GetDebugView()` over HTTP;
- guess sensitivity from key names;
- serialize provider values.

A generic redaction keyword list is not a security boundary.

## Options interfaces

The sample uses:

```text
IOptions<AppOptions>
IOptionsSnapshot<FeatureFlagOptions>
```

`IOptions<T>` doesn't support reading changed configuration values after the
app has started.

`IOptionsSnapshot<T>` is scoped and recomputes options per scope when accessed.
It only reflects post-start configuration changes when the underlying provider
supports those changes.

The full tutorial discusses `IOptionsMonitor<T>` and named options.

## Deliberately omitted

- Azure Key Vault;
- AWS Secrets Manager;
- HashiCorp Vault;
- named options;
- live file reload;
- IOptionsMonitor callbacks;
- raw provider debug trees;
- external services;
- database configuration;
- SMTP configuration.

## Restore, build, and test

```powershell
dotnet restore `
  .\ConfigPrecedenceMinimal.slnx

dotnet build `
  .\ConfigPrecedenceMinimal.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\ConfigPrecedenceMinimal.slnx `
  --configuration Release `
  --no-build
```

## Project structure

```text
ConfigPrecedenceMinimal.slnx
README.md
src/
`-- ConfigPrecedenceMinimal/
    |-- ConfigPrecedenceMinimal.csproj
    |-- Program.cs
    |-- appsettings.json
    |-- appsettings.Development.json
    |-- Diagnostics/
    |   `-- SafeConfigSnapshot.cs
    |-- Options/
    |   |-- AppOptions.cs
    |   `-- FeatureFlagOptions.cs
    `-- Validation/
        `-- AppOptionsValidator.cs
tests/
`-- ConfigPrecedenceMinimal.Tests/
    |-- ConfigPrecedenceMinimal.Tests.csproj
    `-- ConfigurationTests.cs
```

## Verification

- Target framework: .NET 10
- Application NuGet dependencies: none
- Test count: 8
- External services: none
- Required secret committed to repo: none
- User Secrets: local Development only
- Development diagnostics: allowlisted and redacted
- Last reviewed: 2026-08-07