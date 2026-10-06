# Verified Environment

- Verification timestamp: 2026-10-05T21:36:33+05:30
- .NET SDK: 10.0.401
- Target framework: net10.0
- ModelContextProtocol: 2.2.0
- Microsoft.Extensions.Hosting: 10.0.12
- xunit.v3.mtp-v2: 4.0.1
- Transport: stdio
- Release build: PASS
- Deterministic tests: 13/13 passed; 0 failed; 0 skipped
- MCP stdio end-to-end: PASS
- Runtime transport type: ModelContextProtocol.Client.StdioClientTransport
- Runtime client type: ModelContextProtocol.Client.McpClientImpl
- Discovered tool count: 3

## Verified behaviors

- real MCP client/server handshake;
- exactly three tool definitions discovered;
- get_service_health invoked over MCP stdio;
- get_recent_deployment invoked over MCP stdio;
- get_runbook invoked over MCP stdio;
- unknown tool rejected;
- McpClientTool assignable to AIFunction;
- no LLM or cloud API required.

## Evidence boundary

This verification does not establish HTTP transport behavior, authentication/OAuth, LLM-driven tool selection, Agent Framework integration, production readiness, or coverage of MCP features outside this sample.

Raw local outputs are under erification-results/ and are intentionally gitignored.