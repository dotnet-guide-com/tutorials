# Architecture

```text
HTTP client
   |
   | provider name only
   v
Minimal API
   |
   v
ChatGateway
   |---------------------> ConversationStore
   |
   v
ProviderRegistry
   |
   +--> ollama ----------- IChatClient
   +--> openai ----------- IChatClient
   +--> openrouter ------- IChatClient
   +--> azure-openai ----- IChatClient
                              |
                              v
                    Function-invocation pipeline
```

## Security boundary

The request may select only an already registered provider name. Provider endpoints, models and credentials remain trusted server configuration.

## Conversation rule

A conversation is bound to the provider that created it. The sample rejects attempts to continue that conversation with a different provider instead of silently migrating history.

## Scope boundary

This first sample deliberately excludes automatic failover, model ranking, RAG, embeddings, MCP, Agent Framework, persistent memory, database storage, multi-agent orchestration and write-capable business tools.
