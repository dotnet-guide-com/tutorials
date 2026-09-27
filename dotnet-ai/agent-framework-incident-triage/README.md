# Microsoft Agent Framework in .NET &mdash; Evidence-First Incident Triage Agent

Full tutorial: [Microsoft Agent Framework Tutorial in C#: Build a Tool-Using AI Agent](https://www.dotnet-guide.com/tutorials/dotnet-ai/agent-framework-incident-triage/)

A minimal .NET 10 console companion sample for the Microsoft Agent Framework tutorial. It builds a tool-using AI agent that triages a sample incident using deterministic operational evidence, read-only function tools, and a shared agent session &mdash; with no live model required to build or test it.

> This sample is an independent educational reference and is not an official Microsoft sample or specification.

## What this sample demonstrates

- **`IChatClient` provider boundary** &mdash; provider-specific OpenRouter configuration stays in `Program.cs`; everything else works against the neutral `IChatClient` abstraction.
- **`AIAgent`** &mdash; agent creation, instructions, and tool registration are centralized in a factory.
- **`AgentSession`** &mdash; one session is created for the incident and reused for the same-session follow-up question.
- **Streaming** &mdash; model responses are produced with `RunStreamingAsync` and consumed as `AgentResponseUpdate`.
- **Three read-only function tools** &mdash; `get_service_health`, `get_recent_deployment`, and `get_runbook`, created with `AIFunctionFactory.Create`.
- **Deterministic evidence** &mdash; observed operational facts are loaded from embedded JSON (`services.json`, `deployments.json`).
- **Separate runbook guidance** &mdash; reference investigation guidance is loaded independently from evidence (`runbooks.json`).
- **Missing-field behavior** &mdash; absent evidence stays unknown; the agent is instructed never to invent missing facts.
- **Correlation vs causation** &mdash; the sample shows that temporal correlation does not establish root cause.
- **Deterministic tests without an API key** &mdash; the evidence, guidance, and tool layers are fully unit-tested offline.

## Architecture

```text
Program.cs  →  IChatClient  →  TriageAgentFactory  →  AIAgent  →  AgentSession
                                                                       │
                              ┌───────────────┬───────────────┬────────┘
                              ▼               ▼               ▼
                       ServiceHealthTool  DeploymentTool  RunbookTool
                              │                │              │
                          EvidenceStore    EvidenceStore   RunbookStore
                              │                │              │
                        services.json   deployments.json  runbooks.json
```

See [docs/architecture.md](docs/architecture.md) for the full design discussion.

## Project structure

```text
AgentFrameworkIncidentTriage.slnx
global.json
verified-environment.json
docs/
├── architecture.md
├── evidence-first-contract.md
├── sample-run.md
├── tool-risk-ladder.md
└── verified-environment.md
src/
└── IncidentTriageAgent/
    ├── Program.cs
    ├── Agents/TriageAgentFactory.cs
    ├── Data/ (embedded JSON evidence + runbooks)
    ├── Evidence/ (EvidenceItem, EvidenceStore)
    ├── Guidance/ (RunbookItem, RunbookStore)
    └── Tools/ (ServiceHealthTool, DeploymentTool, RunbookTool)
tests/
└── IncidentTriageAgent.Tests/
    ├── EvidenceStoreTests.cs
    ├── RunbookStoreTests.cs
    └── ToolTests.cs
```

## Prerequisites

- .NET 10 SDK
- An API key is required **only** for live execution. Building and testing require no API key and no internet connection to a model provider.

## Restore, build, and test

```bash
dotnet restore AgentFrameworkIncidentTriage.slnx
dotnet build AgentFrameworkIncidentTriage.slnx --configuration Release --no-restore
dotnet test AgentFrameworkIncidentTriage.slnx --configuration Release --no-build
```

Expected: build with 0 errors, and **14/14 deterministic tests passing**.

## Run without an API key

With no `OPENROUTER_API_KEY` set, the application prints a short notice and exits safely &mdash; it makes no live model call:

```bash
dotnet run --project src/IncidentTriageAgent
```

```
OPENROUTER_API_KEY is not configured.

The project can build and test without an API key.
Run 'dotnet test' to verify the deterministic evidence and tool layers.

Set OPENROUTER_API_KEY only when you want to run the live agent.
```

## Run the live sample

The live path uses OpenRouter's OpenAI-compatible endpoint with model `openai/gpt-4o-mini` (override with `OPENROUTER_MODEL`).

PowerShell:

```powershell
$env:OPENROUTER_API_KEY="YOUR_KEY"
dotnet run --project src/IncidentTriageAgent
```

Bash:

```bash
export OPENROUTER_API_KEY="YOUR_KEY"
dotnet run --project src/IncidentTriageAgent
```

`.env` files are not automatically loaded. Replace `YOUR_KEY` with a real key only when you intend to make a live model request &mdash; never commit it.

## Evidence-first behavior

The agent retrieves operational evidence (for example `E-101`, `E-201`) and separate runbook guidance (`RB-CHECKOUT-03`), and distinguishes what is *known* from what is *inferred*. Missing fields remain unknown, guidance is not evidence, and correlation is not treated as proof of causation. This is the repository-specific **Evidence-First Agent Contract** &mdash; see [docs/evidence-first-contract.md](docs/evidence-first-contract.md).

## Tool risk boundary

All three tools in this sample are **read-only** Level 0 / Level 1 tools: they query deterministic stores and perform no operational write. The agent can recommend investigation steps but cannot restart services, roll back deployments, or modify configuration. See [docs/tool-risk-ladder.md](docs/tool-risk-ladder.md).

## Verified sample run

A controlled live run was captured while the deterministic tests were passing &mdash; see [docs/sample-run.md](docs/sample-run.md). Note that LLM wording is probabilistic and may vary between runs; the deterministic layer is stable and fully tested.

## Important boundary / deliberate exclusions

This first Agent Framework sample deliberately does **not** implement: MCP, RAG, vector search, persistent long-term memory, multi-agent workflows, automatic remediation, write-capable tools, service restart/rollback tools, configuration mutation, human approval workflows, durable orchestration, production hosting, A2A, or AG-UI. The tutorial focuses on provider abstraction, tool calling, session state, deterministic evidence, and evidence-aware reasoning. See [docs/architecture.md](docs/architecture.md) and the full tutorial for the follow-on topics.

## Verification table

| Item | Value |
| --- | --- |
| Target framework | net10.0 |
| SDK baseline | 10.0.401 |
| Microsoft.Agents.AI.OpenAI | 1.22.0 |
| Deterministic tests | 14/14 passing |
| Live provider used for verification | OpenRouter |
| Live model used for verification | openai/gpt-4o-mini |
| Live run verification date | 26 September 2026 |

Environment details: [docs/verified-environment.md](docs/verified-environment.md) &middot; [verified-environment.json](verified-environment.json)

## License

Repository-owned sample code is available under the [MIT License](../../LICENSE).