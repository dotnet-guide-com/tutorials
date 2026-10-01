# Hybrid Search companion sample scope

## Included

- .NET 10 minimal API
- `POST /api/documents`
- `POST /api/search` with keyword/vector/hybrid modes
- `GET /health`
- PostgreSQL generated `tsvector` + GIN
- pgvector `vector(768)` + HNSW `vector_cosine_ops`
- Ollama `nomic-embed-text` through `IEmbeddingGenerator`
- deterministic text chunking
- atomic document replacement after embeddings are generated
- Reciprocal Rank Fusion
- deterministic unit tests
- Docker Compose database
- live end-to-end verification script
- shared repository GitHub Actions restore/build/test CI after publication

## Excluded from v1

- `/api/ask` and answer generation
- RAG prompting
- authentication/authorization
- multi-tenancy
- cloud-provider live requests
- reranking/cross-encoders
- dedicated vector databases
- deployment/IaC

The exclusion of answer generation is deliberate: this repository verifies hybrid retrieval rather than mixing retrieval and LLM generation into one tutorial sample.
