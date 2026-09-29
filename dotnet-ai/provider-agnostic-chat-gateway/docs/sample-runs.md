# Sample Runs

## Deterministic run

The deterministic implementation script runs:

```powershell
dotnet restore ProviderAgnosticChatGateway.slnx
dotnet build ProviderAgnosticChatGateway.slnx --configuration Release --no-restore
dotnet test ProviderAgnosticChatGateway.slnx --configuration Release --no-build
```

A successful run proves the application compiles and the deterministic gateway
tests pass without cloud API keys.

Reference result:

```text
.NET SDK:          10.0.401
Target framework:  net10.0
Build:             0 errors
Tests:             25 total, 25 passed, 0 failed, 0 skipped
```

## Live Ollama verification (29 September 2026)

The authoritative local live verification used the reference provider and model:

- provider: `ollama`
- model: `qwen3.8:27b`
- endpoint: `http://localhost:11434`

Live verification established:

```text
Provider registration           LIVE VERIFIED
Non-streaming chat              LIVE VERIFIED
Conversation continuity         LIVE VERIFIED
SSE streaming                   LIVE VERIFIED
Function/tool invocation        LIVE VERIFIED
Unregistered provider rejection LIVE VERIFIED
```

### Non-streaming chat

Observed non-streaming model output:

```text
GATEWAY_OK
```

### Conversation continuity

Reusing a `conversationId`, the conversation-continuity response correctly
recalled the earlier context:

```text
GATEWAY_OK
```

### SSE streaming

Observed SSE deltas reconstructed:

```text
STREAM_OK
```

### Function/tool invocation

Observed explicit tool-call log:

```text
TOOL CALLED: convert_temperature value=100 from=Celsius to=Fahrenheit result=212
```

Observed tool-enabled response:

```text
100 °C = 212 °F
```

### Unregistered-provider rejection

Observed HTTP result for an unregistered provider:

```text
400 Bad Request
```

LLM-generated wording can vary between runs; the verification records the
observable gateway behavior (tool log, deltas, status codes) rather than exact
prose.