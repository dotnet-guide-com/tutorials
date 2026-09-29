# Provider Matrix

This sample distinguishes three verification levels per provider instead of a
single generic "verified" claim:

- **Construction** — the provider/client-construction and API path compiles (COMPILE VERIFIED).
- **Deterministic** — the provider is constructed and tested without a live model/provider call (DETERMINISTICALLY TESTED).
- **Live** — a real provider/model request was executed successfully (LIVE VERIFIED).

## Publication verification matrix (29 September 2026)

| Provider | Construction | Deterministic | Live |
|---|---|---:|---:|
| Ollama | Verified | Verified | Verified with `qwen3.8:27b` |
| OpenAI | Verified | Verified construction | Not run |
| OpenRouter | Verified | Verified construction | Not run in this publication run |
| Azure OpenAI | Verified | Verified construction | Not run |

Cloud providers (`openai`, `openrouter`, `azure-openai`) were compile-verified
and deterministically constructed/tested, but this local publication run did not
perform live cloud requests. Capability values are gateway configuration
metadata; actual model/provider behavior can still vary by selected model and
service configuration.

## Adapter construction paths

| Provider name | Adapter path | Credential source | Default |
|---|---|---:|---|
| `ollama` | `OllamaApiClient` -> `IChatClient` | none | enabled |
| `openai` | OpenAI `ChatClient` -> `AsIChatClient()` | `OPENAI_API_KEY` | disabled |
| `openrouter` | `OpenAIClient` with compatible endpoint -> `AsIChatClient()` | `OPENROUTER_API_KEY` | disabled |
| `azure-openai` | `AzureOpenAIClient` -> OpenAI chat client -> `AsIChatClient()` | `AZURE_OPENAI_API_KEY` or `DefaultAzureCredential` | disabled |

Only providers enabled in trusted server configuration are registered.