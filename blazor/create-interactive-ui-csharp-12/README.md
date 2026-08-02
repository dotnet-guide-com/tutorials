# Blazor Interactive Todo Dashboard — Minimal Sample

A focused .NET 10 companion showing how Razor components, two-way binding,
`EditForm` validation, scoped state, child-component parameters, event callbacks,
and bUnit tests work together in an Interactive Server Blazor Web App.

## Full tutorial

[Blazor Web Development: Create Interactive UIs with C# 12](https://www.dotnet-guide.com/tutorials/blazor/create-interactive-ui-csharp-12/)

## Framework note

The tutorial explains Blazor on .NET 8 and C# 12.

This companion targets .NET 10 so it can use the DOTNET GUIDE repository's
current SDK and CI toolchain. The Razor component, binding, `EditForm`,
DataAnnotations, scoped-state, `EventCallback`, and component-testing patterns
demonstrated here are the same core Blazor concepts.

## What this sample demonstrates

- a Blazor Web App;
- global Interactive Server rendering;
- a routable Todo Dashboard;
- `EditForm` and `DataAnnotationsValidator`;
- two-way input binding;
- field validation messages;
- a scoped in-memory state service;
- component parameters;
- `EventCallback<int>`;
- `@key` for Todo list items;
- create, toggle, filter, and delete interactions;
- current route-aware Not Found handling;
- bUnit 2.x component tests.

## Interaction flow

```text
browser event
  → Razor component handler
  → validated form or EventCallback
  → scoped TodoState update
  → component re-render
```

## Render-mode boundary

The sample uses Interactive Server.

Events are processed on the server over the Blazor circuit. The browser must
maintain a connection to the application.

This sample does not provide offline support.

## State boundary

`TodoState` is registered as scoped.

In Interactive Server, scoped state is associated with the user's circuit. The
state is not a database and is not durable.

Restarting the application or replacing the circuit resets the sample.

## Prerequisite

- .NET 10 SDK
- a modern browser for manual interaction checks

Check:

```powershell
dotnet --version
```

## Restore, build, and test

```powershell
dotnet restore `
  .\BlazorTodoMinimal.slnx

dotnet build `
  .\BlazorTodoMinimal.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\BlazorTodoMinimal.slnx `
  --configuration Release `
  --no-build
```

## Run

```powershell
dotnet run `
  --project .\src\BlazorTodoMinimal\BlazorTodoMinimal.csproj `
  --urls http://localhost:5140
```

Open:

```text
http://localhost:5140/
```

## Manual checks

- add a valid Todo item;
- submit an invalid short title;
- toggle an item;
- filter active and completed items;
- delete an item;
- navigate to an unknown path.

## Testing boundary

The seven bUnit tests render and interact with Razor components in memory.

They do not launch a browser or test a live SignalR connection. Browser behavior
is covered by the manual verification steps.

## Project structure

```text
BlazorTodoMinimal.slnx
README.md
src/
└── BlazorTodoMinimal/
    ├── BlazorTodoMinimal.csproj
    ├── Program.cs
    ├── TodoState.cs
    └── Components/
        ├── _Imports.razor
        ├── App.razor
        ├── Routes.razor
        ├── Pages/
        │   ├── NotFound.razor
        │   └── Todos.razor
        └── Shared/
            └── TodoList.razor
tests/
└── BlazorTodoMinimal.Tests/
    ├── BlazorTodoMinimal.Tests.csproj
    └── TodoDashboardTests.cs
```

## Deliberately omitted

- Interactive WebAssembly;
- Interactive Auto;
- offline/PWA support;
- JavaScript interop;
- QuickGrid;
- virtualization;
- FluentValidation;
- authentication and authorization;
- EF Core and SQLite;
- external APIs;
- browser automation;
- Docker;
- Azure deployment;
- production persistence.

These topics remain in the full tutorial or another focused companion.

## Verification

- Companion target framework: .NET 10
- Tutorial framework: .NET 8
- Render mode: Interactive Server
- Application NuGet packages: none
- External services required: none
- Database required: none
- API keys required: none
- Expected component tests: 7
- Last reviewed: 2026-08-02

This sample is educational and should be reviewed before production use.