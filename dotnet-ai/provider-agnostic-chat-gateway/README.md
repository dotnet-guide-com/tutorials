# Provider-Agnostic Chat Gateway - Minimal Sample

A small ASP.NET Core sample that registers a local Ollama model and an optional OpenAI model behind the same `Microsoft.Extensions.AI.IChatClient` abstraction.

The HTTP endpoint selects a provider by key without depending on a provider-specific SDK.

## Full tutorial

[Build a switchable multi-provider AI gateway with IChatClient](https://www.dotnet-guide.com/tutorials/dotnet-ai/provider-agnostic-chat-gateway/)

## Companion article

[Common Microsoft.Extensions.AI mistakes](https://www.dotnet-guide.com/articles/dotnet-ai/microsoft-extensions-ai-common-mistakes/)

## Prerequisites

- .NET 10 SDK
- Ollama installed and running locally
- The `qwen3.6:27b` Ollama model
- An OpenAI API key only when testing the optional OpenAI provider

Pull the Ollama model:

```powershell
ollama pull qwen3.6:27b
```

Confirm that the model is installed:

```powershell
ollama list
```

## Run the sample

Open PowerShell in this sample folder and run:

```powershell
dotnet restore
dotnet run --urls http://localhost:5123
```

Keep that PowerShell window open while testing the endpoints.

## Check the root endpoint

Open a second PowerShell window and run:

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri "http://localhost:5123/"
```

The response identifies the sample and lists the available endpoints.

## List the registered providers

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri "http://localhost:5123/providers"
```

Without an OpenAI API key, the response should list:

```text
ollama
```

## Send an Ollama request

```powershell
$body = @{
  message = "Explain dependency injection in one sentence."
  provider = "ollama"
} | ConvertTo-Json

Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5123/chat" `
  -ContentType "application/json" `
  -Body $body
```

A successful response includes fields such as:

```text
provider
model
content
finishReason
inputTokens
outputTokens
```

## Optional OpenAI provider

Set the API key only in the current PowerShell session:

```powershell
$env:OpenAI__ApiKey = "YOUR_KEY"
$env:OpenAI__Model = "gpt-4o-mini"

dotnet run --urls http://localhost:5123
```

Then send an OpenAI request:

```powershell
$body = @{
  message = "Explain dependency injection in one sentence."
  provider = "openai"
} | ConvertTo-Json

Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5123/chat" `
  -ContentType "application/json" `
  -Body $body
```

Remove the temporary API key afterward:

```powershell
Remove-Item Env:\OpenAI__ApiKey
```

Never place an API key in:

- `Program.cs`
- `appsettings.json`
- `appsettings.Development.json`
- a README
- a screenshot
- any file uploaded to GitHub

## What this sample demonstrates

- `IChatClient` as a provider-neutral contract
- keyed provider registration with `AddKeyedChatClient`
- local Ollama integration through `OllamaSharp`
- optional OpenAI integration through `.AsIChatClient()`
- provider selection without changing the HTTP endpoint
- non-streaming responses through `GetResponseAsync`
- basic request validation
- usage and finish-reason metadata in the JSON response

## Available endpoints

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `GET` | `/` | Displays basic sample information |
| `GET` | `/providers` | Lists registered AI providers |
| `POST` | `/chat` | Sends a message through the selected provider |

## Example request body

```json
{
  "message": "Explain dependency injection in one sentence.",
  "provider": "ollama"
}
```

When the `provider` value is omitted or empty, the sample uses `ollama` by default.

## Deliberately omitted

The full tutorial adds production-oriented features such as:

- streaming responses
- bounded conversation history
- provider capability metadata
- safe tool calling
- middleware
- OpenTelemetry
- consistent error handling
- automated tests
- resilience and timeout policies

This repository sample intentionally remains small so that the central provider-agnostic design is easy to inspect.

## Project files

```text
provider-agnostic-chat-gateway/
|-- ChatGatewayMinimal.csproj
|-- Program.cs
`-- README.md
```

Generated folders such as `bin` and `obj` should not be committed.

## Verification

- Target framework: .NET 10
- Tested SDK: .NET SDK 10.0.302
- Default local model: `qwen3.6:27b`
- Package versions: see `ChatGatewayMinimal.csproj`
- Local restore: verified
- Release build: verified
- Ollama request: verified
- Last reviewed: 2026-07-24

## Security note

This sample is educational and should be reviewed before production use.

Use environment variables, user secrets, or a managed secrets service for credentials. Never commit API keys or production configuration to a public repository.
