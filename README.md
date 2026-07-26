# dotnet-guide.com - Tutorial Code Samples

Runnable .NET code samples that accompany tutorials published on
[dotnet-guide.com](https://www.dotnet-guide.com/).

Each sample folder contains a focused implementation of one tutorial topic. The full explanation, design reasoning, production considerations, and step-by-step walkthrough remain on the corresponding DOTNET GUIDE tutorial page.

## Available samples

| Sample | What it demonstrates | Full tutorial |
| --- | --- | --- |
| [`dotnet-ai/provider-agnostic-chat-gateway`](dotnet-ai/provider-agnostic-chat-gateway/) | One HTTP chat endpoint using Ollama and optional OpenAI providers through `Microsoft.Extensions.AI.IChatClient` | [Build a switchable multi-provider AI gateway with IChatClient](https://www.dotnet-guide.com/tutorials/dotnet-ai/provider-agnostic-chat-gateway/) |
| [`dotnet-ai/hybrid-search-ef-core-pgvector`](dotnet-ai/hybrid-search-ef-core-pgvector/) | Minimal Reciprocal Rank Fusion demo combining pre-ranked keyword and vector results | [Hybrid Search in .NET with EF Core 10 and pgvector](https://www.dotnet-guide.com/tutorials/dotnet-ai/hybrid-search-ef-core-pgvector/) |
| [`dotnet-aspire/orchestrate-distributed-system`](dotnet-aspire/orchestrate-distributed-system/) | Minimal Aspire AppHost coordinating a web project and API with service discovery and startup ordering | [Aspire in .NET: Orchestrate, Run, and Deploy a Distributed System from One App Host](https://www.dotnet-guide.com/tutorials/dotnet-aspire/orchestrate-distributed-system/) |

## Companion articles

- [Common Microsoft.Extensions.AI mistakes](https://www.dotnet-guide.com/articles/dotnet-ai/microsoft-extensions-ai-common-mistakes/)
- [Vector search in .NET: Common mistakes](https://www.dotnet-guide.com/articles/dotnet-ai/vector-search-common-mistakes/)

## Download and run

1. Click the green **Code** button.
2. Choose **Download ZIP**.
3. Extract the downloaded archive.
4. Open PowerShell in the required sample folder.
5. Follow that folder's `README.md`.

### Provider-agnostic chat gateway

```powershell
cd dotnet-ai\provider-agnostic-chat-gateway
dotnet restore
dotnet run --urls http://localhost:5123
````

### Hybrid search RRF sample

```powershell
cd dotnet-ai\hybrid-search-ef-core-pgvector
dotnet restore
dotnet run
```

## Repository structure

```text
tutorials/
|-- dotnet-ai/
|   |-- provider-agnostic-chat-gateway/
|   |   |-- ChatGatewayMinimal.csproj
|   |   |-- Program.cs
|   |   `-- README.md
|   `-- hybrid-search-ef-core-pgvector/
|       |-- HybridSearchMinimal.csproj
|       |-- Program.cs
|       `-- README.md
|-- dotnet-aspire/
|   `-- orchestrate-distributed-system/
|       |-- AspireOrchestrationMinimal.slnx
|       |-- README.md
|       |-- AspireOrchestrationMinimal.AppHost/
|       |   |-- AppHost.cs
|       |   `-- AspireOrchestrationMinimal.AppHost.csproj
|       |-- AspireOrchestrationMinimal.Api/
|       |   |-- Program.cs
|       |   `-- AspireOrchestrationMinimal.Api.csproj
|       `-- AspireOrchestrationMinimal.Web/
|           |-- Program.cs
|           `-- AspireOrchestrationMinimal.Web.csproj
|-- .github/
|   `-- workflows/
|       `-- build-samples.yml
|-- .gitignore
|-- LICENSE
`-- README.md
```

## Repository boundaries

This repository contains educational code samples only.

It does not contain:

* the DOTNET GUIDE website source code
* production credentials
* analytics configuration
* private deployment paths
* API keys
* complete production applications

The sample folders are intentionally smaller than their corresponding tutorials. Advanced setup, design discussion, database configuration, production hardening, and extended implementation guidance remain on the DOTNET GUIDE website.

## Versions and verification

Each sample identifies:

* its target .NET version
* its required NuGet package versions, when applicable
* its local model, database, or service requirements
* its last-reviewed date
* the commands needed to restore, build, and run it

Some lightweight samples may have no external package dependencies or service requirements.

Libraries and platforms related to AI, authentication, Aspire, distributed systems, databases, and vector search may change quickly. Check the sample's review date and current official documentation before adapting it to a newer SDK or package version.

## Independence

dotnet-guide.com is an independent educational website. It is not affiliated with, endorsed by, or connected to Microsoft Corporation.

Microsoft, .NET, C#, ASP.NET Core, Azure, and related names may be trademarks of Microsoft Corporation. Other product names belong to their respective owners.

## Corrections

Found a compile error or outdated API?

Open an issue and include:

* the sample path
* your .NET SDK version
* the command you ran
* the complete error message
* your operating system

Small and focused pull requests are also welcome.

## License

Repository-owned sample code is available under the [MIT License](LICENSE).

Third-party libraries remain subject to their own licenses.

## Website links

* [DOTNET GUIDE home](https://www.dotnet-guide.com/)
* [Tutorials](https://www.dotnet-guide.com/tutorials/)
* [Articles](https://www.dotnet-guide.com/articles/)
* [Editorial policy](https://www.dotnet-guide.com/editorial/)
* [About DOTNET GUIDE](https://www.dotnet-guide.com/about/)

