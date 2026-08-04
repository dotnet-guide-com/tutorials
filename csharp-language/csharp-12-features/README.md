# C# 12 Everyday Refactoring Lab

A focused .NET 10 console companion demonstrating practical C# 12 refactoring
with primary constructors, explicit public properties, collection expressions,
spread elements, default lambda parameters, and an alias for a tuple type.

## Full tutorial

[C# 12 Language Features: Primary Constructors, Collections & More](https://www.dotnet-guide.com/tutorials/csharp-language/csharp-12-features/)

## Framework and language note

The tutorial introduces C# 12 with the .NET 8 toolchain.

This companion targets .NET 10 because that is the DOTNET GUIDE repository's
current SDK, but both projects explicitly set:

```xml
<LangVersion>12.0</LangVersion>
```

The compiler accepts C# 12 syntax while rejecting newer C# 13 or C# 14 features
accidentally introduced during maintenance.

The project doesn't use `latest` or `preview`.

## What the sample demonstrates

- a class primary constructor;
- explicit properties initialized from constructor parameters;
- validation through a property initializer;
- a service primary constructor;
- collection expressions targeting `List<T>` and arrays;
- spread elements for copying and composition;
- a tuple alias using C# 12 "alias any type";
- a default lambda parameter;
- an explicit override of the lambda default;
- deterministic output;
- seven tests.

## Primary-constructor boundary

Primary-constructor parameters are parameters, not class members.

This class:

```csharp
public sealed class TodoItem(string title)
{
    public string Title { get; } = title;
}
```

has a public `Title` property because the property is explicitly declared.

It doesn't automatically gain a public property named `title`.

The tests protect this distinction with reflection.

## Collection-expression boundary

Collection expressions are target-typed.

These compile because the target type is known:

```csharp
TodoItem[] array = [item];
List<TodoItem> list = [item];
```

A bare declaration such as:

```csharp
var items = [item];
```

doesn't provide a target collection type and isn't used by this sample.

Spread elements enumerate their source.

They should not be described as allocation-free without measurement.

## Alias boundary

```csharp
using TodoCounts = (int Total, int Active, int Completed);
```

creates a source-code alias for a tuple type.

It doesn't create a new nominal runtime type.

Use a record or struct when a distinct domain type is required.

## Default-lambda boundary

The formatter declares its lambda with `var`.

The compiler synthesizes a delegate type that preserves the optional parameter.

The sample calls the lambda both with and without the optional prefix.

## Prerequisite

- .NET 10 SDK

## Restore, build, test, and run

```powershell
dotnet restore `
  .\CSharp12RefactoringLab.slnx

dotnet build `
  .\CSharp12RefactoringLab.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\CSharp12RefactoringLab.slnx `
  --configuration Release `
  --no-build

dotnet run `
  --project .\src\CSharp12RefactoringLab\CSharp12RefactoringLab.csproj `
  --configuration Release `
  --no-build
```

## Expected output

```text
C# 12 Todo Refactoring Lab
Counts: total=4, active=2, completed=2
TODO: Welcome to the C# 12 lab [done]
TODO: Adopt primary constructors [active]
TODO: Use collection expressions [done]
TODO: Try default lambda parameters [active]
TODO: Review explicit properties [done]
```

## Verify the language version

```powershell
dotnet msbuild `
  .\src\CSharp12RefactoringLab\CSharp12RefactoringLab.csproj `
  -getProperty:LangVersion
```

Expected:

```text
12.0
```

## Project structure

```text
CSharp12RefactoringLab.slnx
README.md
src/
`-- CSharp12RefactoringLab/
    |-- CSharp12RefactoringLab.csproj
    |-- Program.cs
    |-- Formatting/
    |   `-- TodoFormatter.cs
    |-- Models/
    |   `-- TodoItem.cs
    `-- Services/
        `-- TodoService.cs
tests/
`-- CSharp12RefactoringLab.Tests/
    |-- CSharp12RefactoringLab.Tests.csproj
    `-- CSharp12FeatureTests.cs
```

## Deliberately omitted

- inline arrays;
- stack allocation;
- unsafe code;
- benchmarks;
- allocation claims;
- Minimal APIs;
- C# 11 list patterns;
- preview features;
- external services.

Inline arrays are advanced struct-based contiguous storage. They deserve a
separate measured sample rather than a contrived use in this Todo application.

## Verification

- Companion target framework: .NET 10
- Explicit language version: C# 12.0
- Tutorial framework: .NET 8
- Application packages: none
- External services required: none
- Expected tests: 7
- Last reviewed: 2026-08-04

This sample demonstrates language semantics. It doesn't claim universal
performance improvements.