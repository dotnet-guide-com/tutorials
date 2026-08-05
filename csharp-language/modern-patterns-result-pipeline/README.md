# C# 12 Result Pipeline Lab

A focused .NET 10 console companion demonstrating typed expected failures,
`Map`, `Bind`, `Match`, one async persistence boundary, invariant validation,
safe error projection, short-circuit behavior, cancellation, deterministic
output, and tests.

## Full tutorial

[C# 12 Functional Patterns: Result Type, Error Handling & Composable Pipeline Testing](https://www.dotnet-guide.com/tutorials/csharp-language/modern-patterns-result-pipeline/)

## Framework and language note

The tutorial is written for C# 12 with the .NET 8 generation.

This companion targets .NET 10 because that is the DOTNET GUIDE repository's
current SDK, but both projects explicitly set:

```xml
<LangVersion>12.0</LangVersion>
```

The sample uses an explicit fallback arm in every switch expression over
`Result<T>`.

C# 12 does not infer a closed hierarchy merely because the base constructor is
private and the known cases are nested and sealed.

## Pipeline

```text
restricted text
  -> Parse
  -> Bind Validate
  -> Map Transform
  -> BindAsync Persist
  -> Match at the edge
```

Expected parse, validation, and configured storage failures are returned as
`Result<T>.Failure`.

Caller cancellation and programming defects remain exceptions.

## Input format

```text
Name,Price,Category,Stock
Widget Pro,9.99,Electronics,50
```

This is a deliberately restricted comma-delimited format.

It does not support:

- quoted fields;
- commas inside values;
- escaped quotes;
- embedded newlines;
- locale-specific decimal separators.

Use a reviewed CSV library when those capabilities are required.

## Error projection

`PipelineError` contains:

```text
Code
Message
Detail
```

`ToPublic()` returns only:

```text
Code
Message
```

`ToDiagnostic(stage)` includes internal detail.

The existence of two projections does not automatically make debug detail safe.

Do not serialize or ship diagnostic detail to external consumers without a
reviewed redaction and sink policy.

## Map and Bind

Use `Map` when validated input is transformed without an expected failure
result.

Use `Bind` when the next stage returns its own `Result`.

Use `BindAsync` to connect the prepared synchronous result to the asynchronous
persistence boundary.

The callbacks can still throw programming exceptions.

## Validation

Numeric values are parsed with `CultureInfo.InvariantCulture`.

Parsed values are carried forward in `ValidatedProduct`, so transformation does
not parse the same text again.

All invalid rows are collected into internal detail.

This sample is fail-whole-batch, not partial-success import.

## Persistence

The in-memory store has an asynchronous-shaped API for composition.

It supports:

- deterministic success;
- deterministic expected rejection;
- cancellation.

It is not a database simulation or throughput benchmark.

## Prerequisite

- .NET 10 SDK

## Restore, build, test, and run

```powershell
dotnet restore `
  .\ResultPipelineLab.slnx

dotnet build `
  .\ResultPipelineLab.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\ResultPipelineLab.slnx `
  --configuration Release `
  --no-build

dotnet run `
  --project .\src\ResultPipelineLab\ResultPipelineLab.csproj `
  --configuration Release `
  --no-build
```

## Expected output

```text
C# 12 Result Pipeline Lab
Success: imported=2, writes=1
Products: Widget Pro | Travel Mug
Failure: code=VALIDATE_BATCH_FAILED, writes=0
Public message: 1 record(s) failed validation.
```

## Project structure

```text
ResultPipelineLab.slnx
README.md
src/
`-- ResultPipelineLab/
    |-- ResultPipelineLab.csproj
    |-- Program.cs
    |-- Core/
    |   |-- Result.cs
    |   `-- ResultExtensions.cs
    |-- Models/
    |   `-- ImportModels.cs
    |-- Persistence/
    |   `-- InMemoryProductStore.cs
    `-- Pipeline/
        |-- ImportPipeline.cs
        |-- ParseStage.cs
        |-- TransformStage.cs
        `-- ValidateStage.cs
tests/
`-- ResultPipelineLab.Tests/
    |-- ResultPipelineLab.Tests.csproj
    `-- ResultPipelineTests.cs
```

## Deliberately omitted

- third-party Result packages;
- FluentAssertions;
- structured logging providers;
- databases;
- ASP.NET Core;
- file uploads;
- full CSV behavior;
- partial success;
- retries;
- exception swallowing.

## Verification

- Companion target framework: .NET 10
- Explicit language version: C# 12.0
- Application NuGet dependencies: none
- External services required: none
- Expected tests: 10
- Expected console lines: 5
- Last reviewed: 2026-08-05

This sample teaches composition and failure semantics. A production Result
abstraction requires decisions about nullability, equality, error accumulation,
serialization, observability, cancellation, exception translation, API
contracts, and team conventions.