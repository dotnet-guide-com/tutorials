\# Provider-Agnostic Chat Gateway â€” Minimal Sample



A small ASP.NET Core sample that registers a local Ollama model and an optional

OpenAI model behind the same `Microsoft.Extensions.AI.IChatClient` abstraction.



The HTTP endpoint selects a provider by key without depending on a provider-specific SDK.



\## Full tutorial



\[Build a switchable multi-provider AI gateway with IChatClient](https://www.dotnet-guide.com/tutorials/dotnet-ai/provider-agnostic-chat-gateway/)



\## Companion article



\[Common Microsoft.Extensions.AI mistakes](https://www.dotnet-guide.com/articles/dotnet-ai/microsoft-extensions-ai-common-mistakes/)



\## Prerequisites



\- .NET 10 SDK

\- Ollama running locally

\- The `qwen3.6:27b` Ollama model

\- An OpenAI API key only when testing the optional OpenAI provider



Pull the model:



```powershell

ollama pull qwen3.6:27b

```



\## Run



```powershell

dotnet restore

dotnet run --urls http://localhost:5123

```



List providers:



```powershell

Invoke-RestMethod `

&#x20; -Method Get `

&#x20; -Uri "http://localhost:5123/providers"

```



Send an Ollama request:



```powershell

$body = @{

&#x20; message = "Explain dependency injection in one sentence."

&#x20; provider = "ollama"

} | ConvertTo-Json



Invoke-RestMethod `

&#x20; -Method Post `

&#x20; -Uri "http://localhost:5123/chat" `

&#x20; -ContentType "application/json" `

&#x20; -Body $body

```



\## Optional OpenAI provider



```powershell

$env:OpenAI\_\_ApiKey = "YOUR\_KEY"

$env:OpenAI\_\_Model = "gpt-4o-mini"

dotnet run --urls http://localhost:5123

```



Never commit API keys to the repository.



\## What this sample demonstrates



\- `IChatClient` as a provider-neutral contract

\- keyed registration with `AddKeyedChatClient`

\- OllamaSharp rather than the deprecated Microsoft Ollama adapter

\- optional OpenAI registration through `.AsIChatClient()`

\- current `GetResponseAsync` usage



\## Deliberately omitted



The full tutorial adds streaming, bounded conversation history, capability metadata,

safe tool calling, middleware, OpenTelemetry, consistent errors, and automated tests.



\## Verification



\- Target framework: .NET 10

\- Package versions: see `ChatGatewayMinimal.csproj`

\- Last reviewed: 2026-07-24



This sample is educational and should be reviewed before production use.

