# Evidence-First Incident Triage Agent — Architecture

This document describes the architecture of the
**Evidence-First Incident Triage Agent** reference implementation.

The sample demonstrates how to combine:

- .NET 10;
- Microsoft Agent Framework;
- `IChatClient`;
- `AIAgent`;
- `AgentSession`;
- deterministic operational evidence;
- read-only function tools;
- reference runbook guidance;
- streaming model responses.

The central design goal is simple:

> Keep deterministic facts separate from probabilistic model reasoning.

---

# Architecture at a Glance

```text
                         ┌──────────────────────────┐
                         │        Program.cs        │
                         │                          │
                         │ Provider configuration   │
                         │ OpenRouter endpoint      │
                         │ model selection          │
                         └────────────┬─────────────┘
                                      │
                                      ▼
                              ┌───────────────┐
                              │  IChatClient  │
                              └───────┬───────┘
                                      │
                                      ▼
                         ┌──────────────────────────┐
                         │   TriageAgentFactory     │
                         │                          │
                         │ Agent instructions       │
                         │ Tool registration        │
                         └────────────┬─────────────┘
                                      │
                                      ▼
                               ┌────────────┐
                               │  AIAgent   │
                               └─────┬──────┘
                                     │
                                     ▼
                              ┌──────────────┐
                              │ AgentSession │
                              └──────┬───────┘
                                     │
                     ┌───────────────┼────────────────┐
                     │               │                │
                     ▼               ▼                ▼
          ┌──────────────────┐ ┌──────────────┐ ┌────────────────┐
          │ ServiceHealthTool│ │DeploymentTool│ │  RunbookTool   │
          └─────────┬────────┘ └──────┬───────┘ └───────┬────────┘
                    │                 │                 │
                    └──────────┬──────┘                 │
                               ▼                        ▼
                      ┌─────────────────┐      ┌─────────────────┐
                      │  EvidenceStore  │      │  RunbookStore   │
                      └────────┬────────┘      └────────┬────────┘
                               │                        │
                     ┌─────────┴─────────┐              │
                     ▼                   ▼              ▼
              services.json      deployments.json   runbooks.json
```

---

# Design Principle 1 — Provider-specific code stays at the edge

The application currently uses OpenRouter through an
OpenAI-compatible endpoint.

That configuration exists only in:

```text
Program.cs
```

The provider-specific path is:

```text
OpenRouter
    ↓
OpenAI-compatible endpoint
    ↓
OpenAIClient
    ↓
AsIChatClient()
    ↓
IChatClient
```

Everything after `IChatClient` is independent of the OpenRouter
configuration.

This means the core agent factory does not need to know:

- the API endpoint;
- the API key;
- the model identifier;
- how the provider SDK was configured.

The architectural boundary is:

```text
provider-specific code
        ↓
     IChatClient
        ↓
provider-neutral agent code
```

---

# Design Principle 2 — Agent creation is centralized

The agent is constructed by:

```text
Agents/TriageAgentFactory.cs
```

The factory receives:

```text
IChatClient
EvidenceStore
RunbookStore
```

and creates the configured:

```text
AIAgent
```

The factory is responsible for:

- agent name;
- agent instructions;
- evidence-first rules;
- tool registration.

It is not responsible for:

- API keys;
- provider endpoints;
- model selection;
- reading JSON directly;
- console interaction.

This keeps agent policy separate from application startup code.

---

# Design Principle 3 — Evidence exists independently of the model

Observed operational facts are stored in:

```text
Data/services.json
Data/deployments.json
```

They are loaded through:

```text
EvidenceStore
```

For example:

```text
E-101
service=checkout-api
status=degraded
error_rate=18.7%
database_pool_utilization=96%
```

exists whether or not an LLM is available.

The model does not create this evidence.

It only receives the evidence through controlled tools.

This allows the deterministic layer to be tested without:

- an API key;
- an Internet connection;
- a model request;
- probabilistic output.

---

# Design Principle 4 — Missing evidence remains missing

The evidence model does not invent values for fields that are absent.

For example:

```text
E-201
deployment=deploy-1842
previous_deployment=deploy-1839
```

contains the identity of the previous deployment.

It does not contain:

```text
previous_deployment_completed_at
```

Therefore:

```text
completion time of deploy-1839 = UNKNOWN
```

The deterministic layer returns `null` for a missing fact.

The agent instructions then reinforce the same boundary:

> Never invent or fill in a missing field.

This is one of the core behaviors tested by the sample.

---

# Design Principle 5 — Evidence and guidance use separate stores

The project deliberately separates:

```text
EvidenceStore
```

from:

```text
RunbookStore
```

because they represent different information classes.

## EvidenceStore

Contains observed records such as:

```text
service health
deployment history
timestamps
error rates
resource utilization
```

Example IDs:

```text
E-101
E-201
```

## RunbookStore

Contains reference guidance such as:

```text
what to inspect
which metrics to compare
which traces to review
```

Example ID:

```text
RB-CHECKOUT-03
```

This distinction prevents architecture like:

```text
runbook recommendation
        ↓
treated as observed fact
```

The desired relationship is:

```text
evidence
   ↓
what is known

guidance
   ↓
what should be checked next
```

---

# Design Principle 6 — Tools are thin adapters

The three tools are:

```text
ServiceHealthTool
DeploymentTool
RunbookTool
```

Their job is intentionally small.

They:

1. receive model-selected arguments;
2. query a deterministic store;
3. return structured text;
4. perform no operational write.

They do not contain model reasoning.

For example:

```text
ServiceHealthTool
       ↓
EvidenceStore
       ↓
E-101
```

The model decides how to interpret E-101.

The tool decides only which record is returned.

---

# Tool Boundary

The current reference implementation exposes:

| Tool | Information class | Risk level | Writes state? |
|---|---|---:|---|
| `get_runbook` | Reference guidance | 0 | No |
| `get_service_health` | Observed evidence | 1 | No |
| `get_recent_deployment` | Observed evidence | 1 | No |

The risk levels correspond to the
[AI Agent Tool Risk Ladder](tool-risk-ladder.md).

No tool in this sample can:

```text
restart a service
rollback a deployment
modify configuration
deploy code
delete data
```

The agent can recommend investigation steps.

It cannot perform remediation.

---

# Agent Execution Flow

A live execution follows this sequence:

```text
User incident prompt
        │
        ▼
     AIAgent
        │
        │ decides which information is needed
        │
        ├──────────────► get_service_health
        │                       │
        │                       ▼
        │                     E-101
        │
        ├──────────────► get_recent_deployment
        │                       │
        │                       ▼
        │                     E-201
        │
        └──────────────► get_runbook
                                │
                                ▼
                         RB-CHECKOUT-03
                                │
                                ▼
                     model interpretation
                                │
                                ▼
                      streamed assessment
```

The model is allowed to interpret the retrieved information.

It is not allowed to redefine the underlying evidence.

---

# Session Flow

The application creates:

```text
AgentSession
```

once for the incident.

The same session is then used for:

```text
initial incident assessment
        ↓
same-session follow-up
```

The reference scenario asks:

```text
Can I truthfully write:

"deploy-1842 caused the checkout-api outage"?
```

Because the previous evidence is already part of the session context,
the follow-up can be answered without reconstructing the entire incident
prompt.

The verified run rejected the causal statement because the evidence
showed timing correlation but did not establish root cause.

---

# Streaming

Live responses are produced through:

```text
RunStreamingAsync(...)
```

and consumed as:

```text
AgentResponseUpdate
```

The console therefore receives model output progressively rather than
waiting for one complete response object.

Streaming affects delivery.

It does not change the evidence model or evidence-first rules.

---

# Deterministic vs Probabilistic Components

The architecture intentionally contains both.

## Deterministic

```text
services.json
deployments.json
runbooks.json
EvidenceStore
RunbookStore
tool lookup behavior
missing-field behavior
tool construction
```

These components are tested with xUnit.

At the verified baseline:

```text
14 tests
14 passed
0 failed
```

## Probabilistic

```text
tool selection by the model
wording of the response
reasoning phrasing
ordering of explanations
whether identical information is requested more than once
```

These behaviors may vary between model runs.

That is why the repository does not make live LLM output a required CI
assertion.

---

# Test Boundary

The normal test path is:

```text
dotnet test
```

It does not require:

```text
OPENROUTER_API_KEY
```

This is deliberate.

CI should be able to verify:

```text
evidence
stores
tools
missing fields
function construction
```

without paying for or depending on a model provider.

The live-model layer is verified separately.

---

# Secret Boundary

The repository contains no real API key.

For a live run the application reads:

```text
OPENROUTER_API_KEY
```

from the environment.

Optional model override:

```text
OPENROUTER_MODEL
```

The default model identifier used during verification was:

```text
openai/gpt-4o-mini
```

Secrets belong outside:

```text
source files
JSON sample data
README examples
tests
committed configuration
```

---

# Repository Layers

The application is organized into the following responsibilities:

```text
src/IncidentTriageAgent/

├── Agents/
│   └── TriageAgentFactory.cs
│
│   Agent policy and tool composition
│
├── Evidence/
│   ├── EvidenceItem.cs
│   └── EvidenceStore.cs
│
│   Deterministic observed facts
│
├── Guidance/
│   ├── RunbookItem.cs
│   └── RunbookStore.cs
│
│   Deterministic reference guidance
│
├── Tools/
│   ├── ServiceHealthTool.cs
│   ├── DeploymentTool.cs
│   └── RunbookTool.cs
│
│   Read-only model-callable adapters
│
├── Data/
│   ├── services.json
│   ├── deployments.json
│   └── runbooks.json
│
│   Reproducible sample data
│
└── Program.cs

    Provider configuration and application execution
```

Tests live separately:

```text
tests/IncidentTriageAgent.Tests/
```

Reference documentation lives under:

```text
docs/
```

---

# Why the sample does not call production monitoring APIs

The sample deliberately uses deterministic JSON instead of:

```text
Azure Monitor
Application Insights
Datadog
Grafana
Kubernetes
production databases
```

This keeps the tutorial reproducible.

Every reader starts with the same incident:

```text
14:02  deploy-1842 completed
14:09  checkout-api error spike began
14:12  checkout-api observed degraded
       error rate = 18.7%
       database pool = 96%
```

Only the model interpretation is probabilistic.

The evidence remains fixed.

A production implementation could later replace the stores with adapters
for live telemetry systems without changing the core evidence-first
design.

---

# What This Architecture Deliberately Excludes

The first reference sample does not implement:

```text
MCP
RAG
vector search
persistent long-term memory
multi-agent workflows
automatic remediation
write-capable tools
human approval workflows
durable orchestration
production hosting
A2A
AG-UI
```

These are intentionally separate topics.

The goal of Agent Framework #1 is to establish:

```text
provider abstraction
        +
tool calling
        +
session state
        +
deterministic evidence
        +
evidence-aware reasoning
```

before introducing more advanced capabilities.

---

# Extension Points

The architecture can later evolve without discarding the core pattern.

For example:

```text
EvidenceStore
    ↓
replace with
Azure Monitor adapter
```

or:

```text
RunbookStore
    ↓
replace with
MCP-backed knowledge tool
```

or:

```text
read-only agent
    ↓
add
approval-controlled operational tool
```

The important boundary remains:

```text
source of truth
    ↓
controlled tool
    ↓
agent interpretation
```

---

# Related Reference Documents

See:

```text
evidence-first-contract.md
```

for the reasoning contract.

See:

```text
tool-risk-ladder.md
```

for the tool-capability classification.

See:

```text
verified-environment.md
```

for the exact environment in which the implementation was tested.

---

## Status

**Implementation:** verified reference sample

**Target framework:** .NET 10

**Agent framework package:** Microsoft.Agents.AI.OpenAI 1.22.0

**Automated tests:** 14 passing

**Live Agent Framework run:** verified

**Last verified:** 26 September 2026