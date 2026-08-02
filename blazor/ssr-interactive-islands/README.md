# Blazor Streaming SSR and Interactive Cart Island

A focused .NET 10 companion showing how a Blazor Web App can keep a product
catalog statically server-rendered, stream delayed reviews, and apply
Interactive Server only to a small cart island.

## Full tutorial

[Blazor SSR & Interactive Islands: Streaming Rendering, Auto Render Mode & Progressive Enhancement](https://www.dotnet-guide.com/tutorials/blazor/ssr-interactive-islands/)

## Framework note

The tutorial explains Blazor render modes on .NET 8.

This companion targets .NET 10 so it can use the DOTNET GUIDE repository's
current SDK and CI toolchain. The static SSR, streaming-rendering,
serializable-boundary, Interactive Server island, and component-testing
concepts demonstrated here are the same core Blazor patterns.

For streaming attributes:

```text
.NET 8:  [StreamRendering(true)]
.NET 9+: [StreamRendering]
```

## What this sample demonstrates

- a statically rendered catalog route;
- server-rendered product content and metadata;
- `[StreamRendering]`;
- an initial reviews placeholder;
- a delayed streamed review update;
- one Interactive Server cart island;
- JSON-serializable parameters crossing the render-mode boundary;
- add, increment, decrement, and remove interactions;
- deterministic item totals;
- a custom direct-route 404 page;
- bUnit and ASP.NET Core integration tests.

## Render boundaries

```text
Product catalog: Static Server
Reviews:         Static SSR + streaming update
Cart:            Interactive Server island
```

The app registers Interactive Server support but does not make the route tree
globally interactive.

## Why this sample excludes Auto and WebAssembly

Interactive WebAssembly and Interactive Auto require a separate `.Client`
project and client-compatible component and service implementations.

This lightweight companion keeps one project so the render boundary remains
easy to inspect and test.

The complete tutorial discusses the broader hybrid architecture.

## Serializable island parameters

The static catalog passes a `ProductSummary[]` value to the cart island.

Parameters crossing from a static parent to an interactive child must be JSON
serializable. Render fragments and arbitrary runtime services cannot cross this
boundary.

## Streaming demonstration delay

The catalog service intentionally waits about 700 milliseconds before returning
featured reviews.

This delay exists only to make the streaming phases visible. It is not a
production recommendation.

## Buffering limitation

Streaming requires the host and intermediaries to let response data flow as it
is generated.

If a reverse proxy buffers the response, the page still renders correctly, but
the placeholder and final review content may appear together.

## Cart-state boundary

Cart state exists only inside the Interactive Server component.

It resets when the component or circuit is replaced.

The sample does not provide persistence, checkout, cross-tab state, or offline
support.

## Prerequisites

- .NET 10 SDK
- a modern browser for optional manual checks
- Python 3 only for the optional incremental-response observation command

## Restore, build, and test

```powershell
dotnet restore `
  .\BlazorCatalogIslands.slnx

dotnet build `
  .\BlazorCatalogIslands.slnx `
  --configuration Release `
  --no-restore

dotnet test `
  .\BlazorCatalogIslands.slnx `
  --configuration Release `
  --no-build
```

## Run

```powershell
dotnet run `
  --project .\src\BlazorCatalogIslands\BlazorCatalogIslands.csproj `
  --urls http://localhost:5148
```

Open:

```text
http://localhost:5148/
```

## Observe the streamed response

Start the application:

```powershell
dotnet run `
  --project .\src\BlazorCatalogIslands\BlazorCatalogIslands.csproj `
  --urls http://localhost:5148
```

Run this Python 3 incremental HTTP command in a separate terminal:

```bash
python3 - <<'PY'
import http.client
import time

connection = http.client.HTTPConnection(
    "127.0.0.1",
    5148,
    timeout=10,
)

connection.request("GET", "/")
response = connection.getresponse()

started = time.monotonic()
buffer = ""
seen = set()

markers = (
    "Loading featured reviews",
    "The keyboard feels excellent for long coding sessions.",
)

while True:
    chunk = response.read(128)
    if not chunk:
        break
    buffer += chunk.decode("utf-8", errors="ignore")
    for marker in markers:
        if marker in buffer and marker not in seen:
            elapsed = time.monotonic() - started
            print(f"{elapsed:.3f}s  {marker}")
            seen.add(marker)

connection.close()
PY
```

Expected output (exact timings vary):

```text
0.000s  Loading featured reviews
0.696s  The keyboard feels excellent for long coding sessions.
```

The marker order is the important result: the loading placeholder
appears first, and the final review text arrives separately on the
same connection.

If a reverse proxy or Kestrel host buffers the complete response, both
markers may arrive together. The page still renders correctly, but the
visible streaming benefit can disappear.

This command observes response flow but is not a load or performance
test.

## Testing boundary

The ten tests cover:

- product rendering;
- review loading and completed states;
- cart interactions and totals;
- streaming-attribute configuration;
- complete HTTP output;
- direct unknown-route handling.

They don't launch a graphical browser or measure network chunk timing.

## Project structure

```text
BlazorCatalogIslands.slnx
README.md
src/
└── BlazorCatalogIslands/
    ├── BlazorCatalogIslands.csproj
    ├── Program.cs
    ├── Models/
    │   └── CatalogModels.cs
    ├── Services/
    │   └── CatalogService.cs
    └── Components/
        ├── _Imports.razor
        ├── App.razor
        ├── Routes.razor
        ├── Pages/
        │   ├── Catalog.razor
        │   └── NotFound.razor
        └── Shared/
            ├── CartIsland.razor
            ├── ProductCard.razor
            └── ReviewsSection.razor
tests/
└── BlazorCatalogIslands.Tests/
    ├── BlazorCatalogIslands.Tests.csproj
    └── CatalogIslandTests.cs
```

## Deliberately omitted

- Interactive WebAssembly;
- Interactive Auto;
- offline support;
- JavaScript interop;
- APIs;
- databases;
- cart persistence;
- structured data;
- load testing;
- WebAssembly AOT;
- Docker;
- cloud deployment.

These topics remain in the full tutorial.

## Verification

- Companion target framework: .NET 10
- Tutorial framework: .NET 8
- Route render mode: Static Server
- Island render mode: Interactive Server
- Application NuGet packages: none
- External services required: none
- Database required: none
- API keys required: none
- Expected tests: 10
- Last reviewed: 2026-08-02

This sample is educational and should be reviewed before production use.