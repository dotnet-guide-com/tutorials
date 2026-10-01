# Verified Environment

## Isolated baseline established before companion generation

The verification run on 2026-09-30 established:

- .NET SDK 10.0.401 / `net10.0`
- EF Core 10.0.12
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3
- Pgvector.EntityFrameworkCore 0.3.0
- Microsoft.Extensions.AI 10.10.0
- Microsoft.Extensions.AI.OpenAI 10.10.1
- Azure.AI.OpenAI 2.1.0
- OllamaSharp 5.5.0
- Release build: 0 warnings, 0 errors
- deterministic verifier tests: 6 passed
- PostgreSQL full-text search: live verified
- GIN index: live verified
- pgvector cosine retrieval: live verified
- HNSW + `vector_cosine_ops`: live verified
- Ollama `nomic-embed-text`: live verified, 768 dimensions
- OpenAI adapter: compile/construction verified only
- Azure OpenAI adapter: compile/construction verified only

## Final companion verification

The publication source must pass `scripts/verify.ps1` after any pre-publication source change. A successful run proves, on the final companion source:

- Release build with warnings treated as errors
- deterministic unit tests
- PostgreSQL 17 + pgvector startup
- real HTTP document ingestion using Ollama embeddings
- keyword retrieval
- vector cosine retrieval
- hybrid Reciprocal Rank Fusion retrieval
- GIN index presence
- HNSW `vector_cosine_ops` index presence
- stored vector dimension = 768

On success, the script updates the repository's `verified-environment.json` with the latest final-source verification timestamp and status.

The default cloud adapters remain compile/construction verified only; no OpenAI or Azure OpenAI live request is claimed.
