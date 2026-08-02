# Blazor Profile Validation Pipeline — Minimal Sample

A focused .NET 10 Interactive Server companion demonstrating manual
`EditContext` ownership, DataAnnotations, FluentValidation, backend field-error
mapping, accessible reusable inputs, dirty-state tracking, save/discard behavior,
and bUnit component tests.

## Full tutorial

[Blazor .NET 8 Forms & Validation: EditForm, FluentValidation & Server Error Handling](https://www.dotnet-guide.com/tutorials/blazor/forms-validation-masterclass/)

## Framework note

The tutorial explains Blazor forms on .NET 8.

This companion targets .NET 10 so it can use the DOTNET GUIDE repository's
current SDK and CI toolchain. The `EditContext`, `ValidationMessageStore`,
DataAnnotations, FluentValidation, field-error mapping, dirty-state,
reusable-input, and component-testing patterns demonstrated here are the same
core Blazor concepts.

## What this sample demonstrates

- a Blazor Web App using Interactive Server;
- a manually created `EditContext`;
- DataAnnotations validation;
- FluentValidation 12 rules;
- a local FluentValidation-to-EditContext bridge;
- whole-model validation on submit;
- property-specific validation on field change;
- backend-returned field errors;
- `ValidationMessageStore`;
- clearing stale backend errors after field edits;
- reusable accessible text inputs;
- `aria-invalid` and `aria-describedby`;
- dirty-state detection with `IsModified()`;
- save and discard behavior;
- `MarkAsUnmodified()` after successful save;
- eight bUnit tests;
- one ASP.NET Core unknown-route integration test.

## Why this sample does not use Blazored.FluentValidation

FluentValidation doesn't provide first-party Blazor integration.

The formerly common `Blazored.FluentValidation` adapter is archived.

This sample keeps the integration visible by using a small local component based
on:

- `EditContext.OnValidationRequested`;
- `EditContext.OnFieldChanged`;
- `ValidationMessageStore`;
- `IValidator<T>`;
- `IncludeProperties`.

The bridge supports synchronous FluentValidation rules only.

## Validation sources

```text
DataAnnotations
  required, length, format

FluentValidation
  conditional and cross-field rules

ProfileService
  backend-only reserved username and blocked email-domain rules
```

Each source owns a separate validation-message store.

## Backend boundary

`ProfileService` is an in-process backend simulation.

It returns a dictionary of field names and messages so the component can
demonstrate backend-error mapping.

It does not make HTTP requests or deserialize RFC 7807 responses.

## Render-mode boundary

The sample uses Interactive Server.

The browser must maintain an active Blazor circuit.

## State boundary

The profile service is scoped and stores data only in process memory.

Restarting the application resets the saved profile.

## Prerequisite

- .NET 10 SDK
- a modern browser for optional manual interaction checks

## Restore, build, and test

```powershell
dotnet restore `
  .\BlazorProfileValidation.slnx

dotnet build `
  .\BlazorProfileValidation.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\BlazorProfileValidation.slnx `
  --configuration Release `
  --no-build
```

## Run

```powershell
dotnet run `
  --project .\src\BlazorProfileValidation\BlazorProfileValidation.csproj `
  --urls http://localhost:5144
```

Open:

```text
http://localhost:5144/
```

## Demonstration backend errors

Use these valid client-side values to trigger backend-only errors:

```text
Username: reserved
Email: any-address@blocked.example
```

## Testing boundary

The test suite contains:

- eight bUnit component tests;
- one ASP.NET Core integration test for direct unknown-route handling.

The tests do not launch a graphical browser or establish a real browser-driven
SignalR session.

## Project structure

```text
BlazorProfileValidation.slnx
README.md
src/
└── BlazorProfileValidation/
    ├── BlazorProfileValidation.csproj
    ├── Program.cs
    ├── Models/
    │   └── ProfileModel.cs
    ├── Services/
    │   └── ProfileService.cs
    ├── Validation/
    │   └── ProfileValidator.cs
    └── Components/
        ├── _Imports.razor
        ├── App.razor
        ├── Routes.razor
        ├── Pages/
        │   ├── NotFound.razor
        │   └── ProfileSettings.razor
        ├── Shared/
        │   └── FormTextField.razor
        └── Validation/
            └── FluentValidationBridge.razor
tests/
└── BlazorProfileValidation.Tests/
    ├── BlazorProfileValidation.Tests.csproj
    └── ProfileSettingsTests.cs
```

## Deliberately omitted

- third-party Blazor validation adapters;
- async validation;
- username API calls;
- debounce;
- JavaScript focus management;
- navigation guards;
- optimistic UI;
- authentication;
- databases;
- WebAssembly;
- browser automation;
- Docker;
- production persistence.

These topics remain in the complete tutorial.

## Verification

- Companion target framework: .NET 10
- Tutorial framework: .NET 8
- Render mode: Interactive Server
- FluentValidation integration: local bridge
- External services required: none
- Database required: none
- API keys required: none
- Expected tests: 9
- Last reviewed: 2026-08-02

This sample is educational and should be reviewed before production use.