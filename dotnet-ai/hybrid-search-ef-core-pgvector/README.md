# Hybrid Search in .NET with EF Core 10 and pgvector

Companion sample for the DOTNET GUIDE tutorial:

`https://www.dotnet-guide.com/tutorials/dotnet-ai/hybrid-search-ef-core-pgvector/`

This sample combines PostgreSQL full-text search and pgvector cosine retrieval, then fuses the two ranked lists with Reciprocal Rank Fusion (RRF).

## Verified default stack

- .NET SDK 10.0.401 / `net10.0`
- EF Core 10.0.12
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3
- Pgvector.EntityFrameworkCore 0.3.0
- PostgreSQL 17 + pgvector (`pgvector/pgvector:pg17`)
- Microsoft.Extensions.AI 10.10.0
- OllamaSharp 5.5.0
- Ollama `nomic-embed-text`
- vector dimension: 768

## API

- `GET /health`
- `POST /api/documents`
- `POST /api/search` (`keyword`, `vector`, `hybrid`)

## Build and deterministic tests

```powershell
dotnet restore .\HybridSearch.slnx
dotnet build .\HybridSearch.slnx --configuration Release --no-restore
dotnet test .\HybridSearch.slnx --configuration Release --no-build
```

## End-to-end verification

Prerequisites:

1. Docker Desktop running.
2. Ollama running at `http://localhost:11434`.
3. `nomic-embed-text` installed (`ollama pull nomic-embed-text`).

Then run:

```powershell
.\scripts\verify.ps1
```

The verification script starts a disposable pgvector PostgreSQL database, runs the API, ingests sample documents through HTTP, exercises all three search modes, confirms GIN/HNSW indexes, checks that stored vectors have 768 dimensions, and writes the latest successful result to `verified-environment.json`.

Use `-KeepRunning` if you want the database left running after a successful verification:

```powershell
.\scripts\verify.ps1 -KeepRunning
```

## Database bootstrap note

`DatabaseInitializer` uses `EnsureCreatedAsync` to keep this tutorial sample reproducible with a clean local Docker database. For a production application, manage schema changes with reviewed EF Core migrations (or another deliberate deployment migration process) rather than using `EnsureCreatedAsync` as the deployment strategy.

## Evidence boundaries

The default Ollama embedding path and PostgreSQL/pgvector path are the live-verification path. OpenAI/Azure OpenAI examples in `EmbeddingProviderFactory` are compile/construction examples only unless a separate credentialed live test is performed.

See `docs/VERIFIED-ENVIRONMENT.md`, `docs/provider-options.md`, and `verified-environment.json`.
