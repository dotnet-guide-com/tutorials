\# Hybrid Search Ranking — Minimal RRF Sample



A small .NET 10 console sample showing how to combine one keyword-ranked list

and one vector-ranked list with Reciprocal Rank Fusion.



\## Full tutorial



\[Hybrid Search in .NET with EF Core 10 and pgvector](https://www.dotnet-guide.com/tutorials/dotnet-ai/hybrid-search-ef-core-pgvector/)



\## What this sample demonstrates



\- two independent ranked candidate lists;

\- rank-based fusion instead of raw-score addition;

\- Reciprocal Rank Fusion;

\- optional keyword and vector weights;

\- deterministic tie-breaking;

\- preservation of keyword and vector source ranks.



\## Important boundary



This repository sample intentionally does \*\*not\*\* reproduce the complete tutorial.



The full DOTNET GUIDE tutorial contains:



\- EF Core 10;

\- PostgreSQL full-text search;

\- pgvector;

\- HNSW;

\- embedding generation;

\- document ingestion;

\- keyword and vector database queries;

\- Minimal API endpoints;

\- optional grounded-answer generation;

\- testing and production guidance.



This sample starts with pre-ranked keyword and vector results so that the

fusion logic can be understood and run without Docker, PostgreSQL, API keys,

embedding models, or external services.



\## Prerequisite



\- .NET 10 SDK



Check:



```powershell

dotnet --version

