# MCP #1 Scope

## In scope

- .NET 10 / SDK 10.0.401
- ModelContextProtocol 2.2.0
- stdio transport
- MCP client/server handshake
- attribute-based tool discovery
- dependency injection into MCP tools
- three deterministic read-only tools
- local JSON-backed data stores
- direct MCP tool invocation
- unknown-tool rejection
- deterministic tests
- local end-to-end verification
- CI-suitable verification without secrets

## Out of scope

- Agent Framework
- IChatClient model execution
- LLM-driven tool selection
- OpenAI / Azure OpenAI / OpenRouter / Ollama
- Streamable HTTP / remote MCP hosting
- OAuth / authentication / authorization
- MCP resources
- MCP prompts
- sampling
- elicitation
- MCP Tasks
- progress notifications
- databases / Redis
- write-capable or destructive tools
- automatic remediation
- RAG / vector search
- multi-agent orchestration
- Docker / Kubernetes / cloud deployment

The goal is to teach and verify the MCP protocol boundary before adding model-driven behavior.
