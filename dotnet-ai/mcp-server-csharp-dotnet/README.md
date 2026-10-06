# MCP in .NET â€” First stdio MCP Server with Read-Only Tools

Full tutorial: [Build Your First MCP Server in C# and .NET](https://www.dotnet-guide.com/tutorials/dotnet-ai/mcp-server-csharp-dotnet/)

This companion sample demonstrates a small, deterministic Model Context Protocol (MCP) workflow in .NET 10. It uses a real MCP client and server over **stdio**, exposes exactly three read-only tools, and verifies discovery and invocation without requiring an LLM, cloud API, API key, database, Docker container, or external service.

> This sample is an independent educational reference from dotnet-guide.com. It is not an official Microsoft or Model Context Protocol specification/sample.

## What this sample demonstrates

- a .NET 10 MCP server hosted with `Microsoft.Extensions.Hosting`;
- `ModelContextProtocol` 2.2.0;
- `AddMcpServer()`, `WithStdioServerTransport()`, and assembly-based tool discovery;
- `[McpServerToolType]` and `[McpServerTool]` tool declarations;
- dependency-injected JSON-backed data stores;
- a real `StdioClientTransport` client;
- `McpClient.CreateAsync(...)`;
- tool discovery with `ListToolsAsync()`;
- direct protocol calls with `CallToolAsync(...)`;
- protocol-level rejection of an unknown tool;
- `McpClientTool` compatibility with `Microsoft.Extensions.AI.AIFunction`;
- deterministic tests that require no model provider.

The three exposed tools are:

```text
get_service_health
get_recent_deployment
get_runbook
```

The data is synthetic and deterministic. `get_service_health` and `get_recent_deployment` return operational evidence records; `get_runbook` returns investigation guidance. Runbook guidance is deliberately kept conceptually separate from incident evidence.

## Architecture

```text
McpClientDemo
    |
    | stdio
    v
FirstMcpServer
    |
    +-- get_service_health
    |      -> ServiceHealthStore
    |
    +-- get_recent_deployment
    |      -> DeploymentStore
    |
    +-- get_runbook
           -> RunbookStore
                |
                v
        deterministic JSON data
```

See [`docs/architecture.md`](docs/architecture.md) for the focused architecture notes and [`docs/protocol-evidence.md`](docs/protocol-evidence.md) for the verification/evidence boundary.

## Project structure

```text
FirstMcpServer.slnx
global.json
verified-environment.json
docs/
scripts/
  verify.ps1
src/
  FirstMcpServer/
    Data/
    Models/
    Stores/
    Tools/
  McpClientDemo/
tests/
  FirstMcpServer.Tests/
```

`src/FirstMcpServer` is the MCP server. `src/McpClientDemo` launches that server as a child process over stdio, discovers the tools, invokes all three, and verifies an invalid tool call is rejected. The test project covers the deterministic store and tool layers.

## Prerequisites

- .NET SDK **10.0.401** or a compatible .NET 10 environment that honors this sample's `global.json`;
- PowerShell only if you want to run the convenience verification script.

No OpenAI, Azure OpenAI, OpenRouter, Ollama, API key, database, or Docker installation is required.

## Restore, build, and test

From this sample directory:

```powershell
dotnet restore .\FirstMcpServer.slnx
dotnet build .\FirstMcpServer.slnx --configuration Release --no-restore
dotnet test .\tests\FirstMcpServer.Tests\FirstMcpServer.Tests.csproj --configuration Release --no-build
```

The verified local baseline is **13/13 deterministic tests passing**, with a Release build at **0 warnings and 0 errors**.

## Run the real MCP stdio demo

Build first, then run:

```powershell
dotnet run `
  --project .\src\McpClientDemo\McpClientDemo.csproj `
  --configuration Release `
  --no-build `
  -- `
  .\src\FirstMcpServer\bin\Release\net10.0\FirstMcpServer.dll
```

The demo launches `FirstMcpServer`, performs the MCP handshake, lists the exposed tools, calls all three tools, deliberately calls `does_not_exist`, confirms the protocol rejects it, and finishes with:

```text
VERIFY|STDIO_END_TO_END|PASS|...
FINAL|PASS
```

For the complete automated local check:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify.ps1
```

That script restores, builds, runs the deterministic tests, captures the package graph, runs the real MCP stdio client/server path, and updates the verification documents.

## Why stdout and stderr matter

With stdio transport, **stdout belongs to the MCP protocol**. Ordinary application diagnostics must not be mixed into that protocol stream. This sample configures host logging so normal diagnostics go to stderr while MCP messages continue over the transport channel.

`McpClientDemo` captures the server's stderr separately and prefixes those lines with `SERVER_STDERR|`. Seeing normal hosting or MCP diagnostic lines there is expected and does not mean the verification failed.

## Environment and secret boundary

The client does not blindly pass every parent-process environment variable to the child MCP server. It disables broad environment inheritance and uses the SDK's safe default environment set required to launch the child process.

This sample has no provider key and no `.env` requirement. Do not add real credentials to source, documentation, tests, committed JSON, CI configuration, or pull-request text.

## Verification

The committed evidence separates what was actually proved from what is merely possible.

| Check | Verified result |
| --- | --- |
| .NET SDK baseline | 10.0.401 |
| Target framework | net10.0 |
| `ModelContextProtocol` | 2.2.0 |
| `Microsoft.Extensions.Hosting` | 10.0.12 |
| `xunit.v3.mtp-v2` | 4.0.1 |
| Release build | PASS â€” 0 warnings, 0 errors |
| Deterministic tests | PASS â€” 13/13 |
| MCP stdio client/server | PASS |
| Discovered tools | PASS â€” exactly 3 |
| All three MCP tool calls | PASS |
| Unknown-tool rejection | PASS |
| `McpClientTool` -> `AIFunction` assignability | PASS |
| LLM/cloud API call | Not required / not performed |

See [`docs/verified-environment.md`](docs/verified-environment.md), [`verified-environment.json`](verified-environment.json), and [`docs/sample-runs.md`](docs/sample-runs.md) for the captured evidence.

The verified local run date is recorded in those generated evidence files rather than hard-coded here.

## Important boundary

This first MCP sample intentionally does **not** implement or verify:

- Agent Framework integration;
- LLM-driven tool selection;
- Streamable HTTP or remote MCP hosting;
- OAuth, authentication, or authorization;
- MCP resources;
- MCP prompts;
- sampling;
- elicitation;
- MCP Tasks;
- progress notifications;
- write-capable or destructive tools;
- databases or Redis;
- RAG/vector search;
- multi-agent orchestration;
- Docker/Kubernetes/cloud deployment;
- production readiness.

Those are valid follow-up topics, but adding them here would blur the purpose of this first protocol-focused sample.

## License

Repository-owned sample code is covered by the repository's [MIT License](../../LICENSE).
