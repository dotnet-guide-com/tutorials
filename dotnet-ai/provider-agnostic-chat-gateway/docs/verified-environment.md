# Verified Environment

## Deterministic baseline (29 September 2026)

- .NET SDK: `10.0.401`
- Target framework: `net10.0`
- Solution format: `.slnx`
- Restore: PASS
- Release build: PASS (0 errors)
- Deterministic tests: 25/25 passed, 0 failed, 0 skipped
- Tool-logging patch applied and re-validated (build + all 25 tests passed again)

## Direct package baseline

- Microsoft.Extensions.AI: 10.10.0
- Microsoft.Extensions.AI.OpenAI: 10.10.1
- Azure.AI.OpenAI: 2.1.0
- Azure.Identity: 1.21.0
- OllamaSharp: 5.4.30

Relevant resolved dependencies recorded during verification:

- Microsoft.Extensions.AI.Abstractions: 10.10.1
- OpenAI: 2.14.0
- System.ClientModel: 1.15.0

## Local live verification (29 September 2026)

- Local live provider: Ollama
- Local live model: `qwen3.8:27b`
- Local live endpoint: `http://localhost:11434`
- Live verification date: 29 September 2026

Live checks (provider registration, non-streaming chat, conversation
continuity, SSE streaming, function/tool invocation, unregistered-provider
rejection) ran against the local Ollama model. Cloud providers were
compile-verified and deterministically constructed/tested but were **not**
live-verified in this publication run.

Normal restore/build/test requires no cloud API key and no running model server.