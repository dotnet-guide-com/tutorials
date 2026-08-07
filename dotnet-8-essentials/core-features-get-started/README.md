# Native AOT Minimal API Essentials

A focused ASP.NET Core companion demonstrating `CreateSlimBuilder`,
source-generated JSON metadata, an AOT-compatible Minimal API, typed DI,
warning-aware native publishing, and direct execution of the resulting Linux
native executable.

## Full tutorial

[.NET 8 Essentials: Core Features & Getting Started](https://www.dotnet-guide.com/tutorials/dotnet-8-essentials/core-features-get-started/)

## Version note

The full tutorial explains features introduced with the .NET 8 generation.

This companion targets .NET 10 because that is the DOTNET GUIDE repository's
current LTS baseline.

As of August 2026:

```text
.NET 10 = active LTS
.NET 8  = maintenance
.NET 8 end of support = November 10, 2026
```

The Native AOT concepts demonstrated here remain directly relevant.

## Focus

```text
CreateSlimBuilder
  -> AOT-compatible DI
  -> Minimal API
  -> source-generated JSON
  -> Native AOT publish
  -> direct native executable
```

This is not another Todo CRUD sample.

## Why CreateSlimBuilder?

The standard:

```csharp
WebApplication.CreateBuilder(args)
```

is still the normal choice for many applications.

This sample uses:

```csharp
WebApplication.CreateSlimBuilder(args)
```

because it starts with a smaller ASP.NET Core feature set designed for
trim/AOT-oriented applications.

It does not imply that every API should use the slim builder.

## JSON source generation

Every request and response body type is registered in:

```text
AppJsonSerializerContext
```

The application does not add a reflection-based serializer fallback.

Native AOT requires application behavior to be statically analyzable; open-ended
runtime reflection and dynamic code generation are constrained.

## Endpoints

### Sample metadata

```http
GET /
```

Expected:

```json
{"sample":"native-aot-essentials","builder":"CreateSlimBuilder","json":"source-generated"}
```

### Runtime capability

```http
GET /runtime
```

The Native AOT executable should report:

```text
dynamicCodeSupported = false
dynamicCodeCompiled = false
```

This is a runtime capability check, not a benchmark.

### Typed echo

```http
POST /echo
Content-Type: application/json

{
  "message": "  hello   native aot  "
}
```

Expected:

```json
{"message":"HELLO NATIVE AOT","length":16}
```

## Native AOT does not mean universally faster

This repository sample intentionally makes no claim such as:

```text
50% faster
70% faster
10 MB executable
half the memory
```

Actual results depend on:

- workload;
- dependencies;
- platform;
- runtime version;
- publish settings;
- request mix;
- measurement method.

Measure your own application.

## Restore, build, and test

```powershell
dotnet restore `
  .\NativeAotEssentialsMinimal.slnx

dotnet build `
  .\NativeAotEssentialsMinimal.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\NativeAotEssentialsMinimal.slnx `
  --configuration Release `
  --no-build
```

## Native publish

Linux x64 example:

```bash
dotnet publish \
  src/NativeAotEssentialsMinimal/NativeAotEssentialsMinimal.csproj \
  --configuration Release \
  --runtime linux-x64
```

Native AOT requires native build prerequisites for the host platform.

Native AOT isn't an arbitrary cross-compilation system.

## AOT limitations

Review AOT/trimming warnings rather than blindly suppressing them.

Common problem areas include:

- dynamic assembly loading;
- runtime code generation;
- unbounded reflection;
- libraries that aren't trim/AOT compatible.

## Deliberately omitted

- controllers;
- MVC;
- database access;
- OpenAPI packages;
- Swagger UI;
- authentication;
- reflection fallbacks;
- benchmarking;
- Docker;
- cloud deployment.

## Project structure

```text
NativeAotEssentialsMinimal.slnx
README.md
src/
`-- NativeAotEssentialsMinimal/
    |-- NativeAotEssentialsMinimal.csproj
    |-- Program.cs
    |-- Models/
    |   `-- ApiModels.cs
    |-- Serialization/
    |   `-- AppJsonSerializerContext.cs
    `-- Services/
        |-- ITextTransformer.cs
        `-- TextTransformer.cs
tests/
`-- NativeAotEssentialsMinimal.Tests/
    |-- NativeAotEssentialsMinimal.Tests.csproj
    `-- NativeAotEssentialsTests.cs
```

## Verification

- Target framework: .NET 10
- Publish model: Native AOT
- Application NuGet packages: none
- Managed tests: 8
- JSON metadata: source generated
- Native CI RID: linux-x64
- External services: none
- Performance claims: none
- Last reviewed: 2026-08-07