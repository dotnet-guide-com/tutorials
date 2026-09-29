# Microsoft.Extensions.AI in .NET — Provider-Agnostic Chat Gateway

Full tutorial: [Microsoft.Extensions.AI Tutorial: Build a Multi-Provider IChatClient Gateway](https://www.dotnet-guide.com/tutorials/dotnet-ai/provider-agnostic-chat-gateway/)

A verified .NET 10 sample that puts multiple AI providers behind the common
`Microsoft.Extensions.AI.IChatClient` boundary without letting HTTP callers
choose arbitrary endpoints, models, or credentials. A caller can select only a
named provider that is registered and enabled in trusted server configuration.

## What this sample demonstrates

- `Microsoft.Extensions.AI.IChatClient` as the application-level provider boundary;
- named, allow-listed provider registration;
- provider-specific SDK construction kept at the application edge;
- Ollama, OpenAI, OpenAI-compatible/OpenRouter, and Azure OpenAI construction paths;
- capability metadata for streaming and tools;
- bounded in-memory, non-persistent conversation history;
- provider-bound conversations that never silently migrate to another provider;
- non-streaming chat;
- SSE streaming;
- a deterministic `convert_temperature` function tool built with `AIFunctionFactory.Create(...)`;
- automatic function invocation through the Microsoft.Extensions.AI pipeline;
- deterministic tests that need no cloud API key.

## Architecture

```text
HTTP caller
   |
   v
ASP.NET Core Minimal API  (Program.cs)
   |
   +--> GET  /api/providers      (list registered providers)
   +--> POST /api/chat           (non-streaming reply)
   +--> POST /api/chat/stream    (SSE stream)
   |
   v
ChatGateway  (gateway logic, tool invocation, temperature tool)
   |
   +--> ConversationStore  (bounded in-memory, provider-bound conversations)
   |
   v
ProviderRegistry  (allow-list of trusted, server-registered providers)
   |
   |  ProviderClientFactory  (constructs the provider-specific IChatClient)
   v
IChatClient  (Microsoft.Extensions.AI provider boundary)
   |
   +--> OllamaClient   (default local provider)
   +--> OpenAIClient
   +--> OpenAI-compatible endpoint (OpenRouter)
   +--> AzureOpenAIClient
```

See [docs/architecture.md](docs/architecture.md) for the full walkthrough.

## Project structure

```text
provider-agnostic-chat-gateway/
|-- ProviderAgnosticChatGateway.slnx
|-- README.md
|-- global.json
|-- verified-environment.json
|-- docs/
|   |-- architecture.md
|   |-- provider-matrix.md
|   |-- sample-runs.md
|   `-- verified-environment.md
|-- src/
|   `-- ProviderAgnosticChatGateway/
|       |-- ProviderAgnosticChatGateway.csproj
|       |-- Program.cs
|       |-- appsettings.json
|       |-- appsettings.Development.json
|       |-- Properties/launchSettings.json
|       |-- Providers/   (ProviderCapabilities, ProviderDescriptor, ProviderRegistry, ProviderClientFactory)
|       |-- Conversations/(ConversationState, ConversationStore)
|       |-- Gateway/     (ChatGateway, ChatRequest, ChatResult, ChatStreamEvent)
|       `-- Tools/       (TemperatureTool)
`-- tests/
    `-- ProviderAgnosticChatGateway.Tests/
        |-- ProviderAgnosticChatGateway.Tests.csproj
        |-- FakeChatClient.cs
        |-- ProviderRegistryTests.cs
        |-- ConversationStoreTests.cs
        |-- ProviderClientFactoryTests.cs
        |-- GatewayTests.cs
        `-- ToolTests.cs
```

## Prerequisites

- .NET 10 SDK (baseline used: `10.0.401`, target framework `net10.0`).
- Ollama only if you want to run the default local live path.
- No cloud API key is required to restore, build, or test this sample.

## Restore, build, and test

```powershell
dotnet restore ProviderAgnosticChatGateway.slnx
dotnet build ProviderAgnosticChatGateway.slnx --configuration Release --no-restore
dotnet test ProviderAgnosticChatGateway.slnx --configuration Release --no-build
```

The verified baseline reports **25/25 deterministic tests passing** with 0 build
errors. See [docs/verified-environment.md](docs/verified-environment.md).

## Run with Ollama

The checked-in default configuration enables the local `ollama` provider:

- endpoint: `http://localhost:11434`
- reference model: `qwen3.8:27b`

If that model is not installed on your machine, change `model` in
`src/ProviderAgnosticChatGateway/appsettings.json`. Only providers enabled in
trusted server configuration are registered.

```powershell
dotnet run --project .\src\ProviderAgnosticChatGateway\ProviderAgnosticChatGateway.csproj
```

## Inspect providers

List the registered providers:

```text
GET /api/providers
```

The response shows the provider names that are currently enabled in server
configuration together with their declared capabilities.

## Non-streaming chat

```text
POST /api/chat
Content-Type: application/json

{
  "provider": "ollama",
  "conversationId": null,
  "message": "Explain IChatClient in one paragraph.",
  "useTools": false
}
```

The reply is returned as a single JSON response.

## Conversation continuity

Reuse a `conversationId` to continue the same provider-bound conversation.
The gateway keeps a bounded, in-memory history and never migrates an existing
conversation to a different provider automatically.

```text
POST /api/chat
Content-Type: application/json

{
  "provider": "ollama",
  "conversationId": "<id-from-previous-response>",
  "message": "Which provider are we using, and what did I ask last?",
  "useTools": false
}
```

## SSE streaming

```text
POST /api/chat/stream
Content-Type: application/json
Accept: text/event-stream

{
  "provider": "ollama",
  "conversationId": null,
  "message": "Count from one to five.",
  "useTools": false
}
```

The stream emits typed events:

- `meta` — session and provider metadata;
- `delta` — each incremental text chunk as it arrives;
- `done` — final event signaling stream completion.

## Function tool

The sample ships one harmless deterministic tool:

```text
convert_temperature(value, from, to)
```

It converts between Celsius and Fahrenheit. Enable it per request with
`"useTools": true`. The Microsoft.Extensions.AI function-invocation pipeline
calls the tool automatically and feeds the result back to the model. The gateway
also logs each real invocation, for example:

```text
TOOL CALLED: convert_temperature value=100 from=Celsius to=Fahrenheit result=212
```

There are no write-capable or unrelated tools.

## Optional cloud providers

Cloud providers are disabled by default. You can enable OpenAI, OpenRouter, and
Azure OpenAI in server configuration. Secrets are read only from the configured
server-side environment-variable names:

- OpenAI: `OPENAI_API_KEY`
- OpenRouter: `OPENROUTER_API_KEY`
- Azure OpenAI API-key mode: `AZURE_OPENAI_API_KEY`

Azure OpenAI may alternatively use `DefaultAzureCredential`. Never place a real
API key in source, documentation, tests, or GitHub Actions. No `.env` file is
required.

## Provider security boundary

HTTP callers may select only provider names registered and enabled in trusted
server configuration. A request is **not** allowed to supply an arbitrary
provider endpoint, model, API key, or credential. Endpoints, models, and secrets
remain trusted server configuration.

## Verification levels

This repository deliberately distinguishes three evidence levels:

- **COMPILE VERIFIED** — the provider construction/API path compiles successfully.
- **DETERMINISTICALLY TESTED** — tests validate gateway behavior without a live model/provider call.
- **LIVE VERIFIED** — a real provider/model request was executed successfully.

See [docs/provider-matrix.md](docs/provider-matrix.md) for the per-provider matrix.

## Verified sample run

See [docs/sample-runs.md](docs/sample-runs.md) for the recorded local run. The
local live verification on **29 September 2026** used Ollama with `qwen3.8:27b`
and established provider registration, non-streaming chat, conversation
continuity, SSE streaming, real tool invocation, and unregistered-provider
rejection. Cloud providers were **not** live-verified in that publication run;
they were compile-verified and deterministically constructed/tested.

## Deliberate exclusions

This first provider-gateway sample deliberately does **not** implement:
Agent Framework/`AIAgent`, MCP, RAG/vector search, embeddings, persistent or
database conversation memory, Redis storage, automatic failover, load balancing,
model ranking/benchmarking, multi-agent orchestration, arbitrary user-supplied
provider endpoints, user-supplied API keys, write-capable business tools,
production authentication, or Azure/Kubernetes deployment infrastructure.

## License

Repository-owned sample code is MIT licensed. See [](../../LICENSE).

> This sample is an independent educational reference and is not an official Microsoft sample or specification.

Provider/model capabilities can vary; this sample does not claim identical
capabilities across all provider/model combinations, and it does not claim live
cloud-provider verification that did not occur.