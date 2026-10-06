# Protocol Evidence

The verification workflow distinguishes several evidence levels.

- **COMPILE VERIFIED** — the pinned project/package/API surface compiles.
- **DETERMINISTICALLY TESTED** — store and tool behavior passes local automated tests.
- **MCP STDIO E2E VERIFIED** — a real MCP client launches a real MCP server, discovers the tools, invokes them over stdio, and observes expected results.
- **CI VERIFIED** — reserved for a later GitHub Actions run after publication.

The sample does not claim LLM-driven tool selection, HTTP transport, authentication, production readiness, or coverage of all MCP features.

`McpClientTool` being assignable to `Microsoft.Extensions.AI.AIFunction` is recorded as an extension point only; the sample does not use an LLM.
