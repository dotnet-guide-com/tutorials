# Verified Environment

This document records the environment in which the
**Evidence-First Incident Triage Agent** reference sample was built,
tested, and manually exercised.

It is a verification record, not a claim that these are the only
compatible versions.

---

# Verification Date

**26 September 2026**

---

# Platform

- Target framework: `net10.0`
- .NET SDK used: `10.0.401`
- Development environment: Windows
- Solution format: `.slnx`

The SDK version was verified with:

```text
dotnet --version
```

Observed result:

```text
10.0.401
```

---

# Direct Package Dependency

The application project has one direct Agent Framework integration
package:

| Package | Requested | Resolved |
|---|---:|---:|
| `Microsoft.Agents.AI.OpenAI` | `1.22.0` | `1.22.0` |

This was installed with:

```text
dotnet add src/IncidentTriageAgent/IncidentTriageAgent.csproj package Microsoft.Agents.AI.OpenAI --version 1.22.0
```

---

# Relevant Resolved Dependencies

The resolved package graph was inspected with:

```text
dotnet list src/IncidentTriageAgent/IncidentTriageAgent.csproj package --include-transitive
```

The following relevant packages were observed:

| Package | Resolved version |
|---|---:|
| `Microsoft.Agents.AI` | `1.22.0` |
| `Microsoft.Agents.AI.Abstractions` | `1.22.0` |
| `Microsoft.Extensions.AI` | `10.10.0` |
| `Microsoft.Extensions.AI.Abstractions` | `10.10.0` |
| `Microsoft.Extensions.AI.Evaluation` | `10.10.0` |
| `Microsoft.Extensions.AI.OpenAI` | `10.10.0` |
| `Microsoft.Extensions.VectorData.Abstractions` | `10.10.0` |
| `OpenAI` | `2.13.0` |
| `System.ClientModel` | `1.15.0` |

Additional transitive dependencies were also resolved by NuGet.

The versions above are recorded because they are directly relevant to
the Agent Framework, Microsoft.Extensions.AI, and OpenAI-compatible
provider path used by this sample.

---

# SDK Selection

The repository includes a `global.json` baseline using:

```json
{
  "sdk": {
    "version": "10.0.401",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

This records the SDK baseline while allowing an appropriate later
installed .NET 10 feature band according to the configured roll-forward
policy.

---

# Build Verification

Command:

```text
dotnet build
```

Observed result:

```text
Build succeeded.
```

Both projects compiled successfully:

```text
src/IncidentTriageAgent/IncidentTriageAgent.csproj
tests/IncidentTriageAgent.Tests/IncidentTriageAgent.Tests.csproj
```

Verification status:

```text
PASS
```

---

# Deterministic Test Verification

Command:

```text
dotnet test
```

Observed test summary:

```text
total: 14
failed: 0
succeeded: 14
skipped: 0
```

Verification status:

```text
PASS
```

The automated test suite does not require:

```text
OPENROUTER_API_KEY
```

and does not require a live model request.

The deterministic tests cover the sample's evidence stores, runbook
store, tool behavior, missing-field behavior, and Agent Framework
function construction.

---

# No-Key Execution Verification

The application was also run without an API key:

```text
dotnet run --project src/IncidentTriageAgent
```

Observed behavior:

```text
OPENROUTER_API_KEY is not configured.

The project can build and test without an API key.
Run 'dotnet test' to verify the deterministic evidence and tool layers.

Set OPENROUTER_API_KEY only when you want to run the live agent.
```

This verifies that cloning, building, and testing the sample does not
require a model-provider secret.

---

# Live Agent Framework Verification

A separate controlled live run was performed after the deterministic
build and tests passed.

Provider:

```text
OpenRouter
```

OpenAI-compatible endpoint:

```text
https://openrouter.ai/api/v1
```

Model identifier used:

```text
openai/gpt-4o-mini
```

Verification status:

```text
PASS
```

The live run verified the following application path:

```text
OpenRouter
    ↓
OpenAI-compatible client
    ↓
AsIChatClient()
    ↓
IChatClient
    ↓
TriageAgentFactory
    ↓
ChatClientAgent
    ↓
AgentSession
    ↓
read-only function tools
    ↓
streamed response
```

---

# Verified Agent Framework Runtime Types

During the compile spike and live verification, the following concrete
runtime types were observed:

```text
Agent type: ChatClientAgent
Session type: ChatClientAgentSession
```

The application code depends on the public abstractions:

```text
AIAgent
AgentSession
IChatClient
```

rather than on the concrete runtime types.

---

# Verified Function Tools

The reference implementation exposes three read-only tools:

```text
get_service_health
get_recent_deployment
get_runbook
```

The live Agent Framework run successfully used the sample's deterministic
information sources:

```text
E-101
E-201
RB-CHECKOUT-03
```

These correspond to:

```text
E-101            service-health evidence
E-201            deployment evidence
RB-CHECKOUT-03   runbook guidance
```

---

# Verified Incident Evidence

The deterministic sample includes:

```text
E-101
evidence_type=service_health
service=checkout-api
observed_at=2026-09-24T14:12:00Z
status=degraded
error_rate=18.7%
error_spike_started=2026-09-24T14:09:00Z
database_pool_utilization=96%
```

and:

```text
E-201
evidence_type=deployment
service=checkout-api
deployment=deploy-1842
deployment_status=succeeded
completed_at=2026-09-24T14:02:00Z
previous_deployment=deploy-1839
change_summary=pricing-rule refresh and structured logging update
rollback_performed=false
```

The evidence intentionally does **not** include a completion timestamp
for:

```text
deploy-1839
```

That omission is used to test whether missing evidence remains unknown.

---

# Missing-Field Verification

During the experimental validation, the agent was explicitly challenged
with:

```text
At exactly what time did deploy-1839 complete?
```

The verified response correctly treated that value as unknown because the
retrieved evidence did not contain the timestamp.

The final live sample also preserved this boundary and reported the
timestamp of `deploy-1839` as unknown.

This verifies the intended behavior:

```text
known identifier
    ≠
known timestamp
```

---

# Correlation vs Causation Verification

The deterministic evidence establishes:

```text
14:02 UTC   deploy-1842 completed
14:09 UTC   checkout-api error spike began
14:12 UTC   checkout-api observed degraded
```

This establishes temporal correlation.

It does not establish that:

```text
deploy-1842 caused the checkout-api outage
```

In the verified same-session follow-up, the agent rejected that causal
statement and explained that the available evidence supports correlation
but not causation.

This behavior is a central part of the
**Evidence-First Agent Contract**.

---

# Guidance vs Evidence Verification

The live sample retrieved:

```text
RB-CHECKOUT-03
```

which recommends checks such as:

```text
inspect database connection-pool utilization
review traces for timeout or connection-acquisition failures
compare configuration with the previous known-good release
```

The verified agent treated these items as investigation guidance rather
than as proof that any one of those conditions caused the incident.

This preserves the intended distinction:

```text
runbook guidance
    ≠
observed incident evidence
```

---

# AgentSession Verification

The same `AgentSession` was reused for:

```text
initial incident assessment
        ↓
same-session follow-up
```

The follow-up asked whether it was truthful to state:

```text
deploy-1842 caused the checkout-api outage
```

The agent answered using the existing session context rather than
requiring the complete incident evidence to be restated by the user.

---

# Streaming Verification

The live sample uses:

```text
RunStreamingAsync(...)
```

and consumes:

```text
AgentResponseUpdate
```

The live run successfully streamed the incident assessment and the
same-session follow-up.

---

# Security and Secret Handling

No real API key is stored in the source code, JSON data files, tests, or
repository documentation.

The application reads the live provider key from:

```text
OPENROUTER_API_KEY
```

An optional model override can be supplied with:

```text
OPENROUTER_MODEL
```

The temporary OpenRouter key used for manual live verification was not
committed to the repository and was intended to be revoked after the
verification run.

---

# Deterministic vs Probabilistic Verification Boundary

The repository deliberately separates deterministic verification from
probabilistic model behavior.

## Deterministically verified

```text
JSON evidence loading
runbook loading
evidence lookup
missing-field behavior
tool output
unknown-service behavior
AIFunction construction
build
unit tests
```

These behaviors are suitable for automated CI verification.

## Manually observed live behavior

```text
model tool selection
model-generated wording
reasoning phrasing
same-session interpretation
correlation-vs-causation response
```

These behaviors are probabilistic and may vary between model runs.

For that reason, live LLM output is not treated as a deterministic CI
assertion.

---

# Current Verification Summary

| Item | Verified |
|---|---|
| .NET SDK `10.0.401` | Yes |
| Target framework `net10.0` | Yes |
| `.slnx` solution | Yes |
| `Microsoft.Agents.AI.OpenAI 1.22.0` | Yes |
| Build | PASS |
| Automated tests | 14/14 PASS |
| Build without API key | Yes |
| Tests without API key | Yes |
| Safe no-key application exit | Yes |
| `IChatClient` provider abstraction | Yes |
| `ChatClientAgent` creation | Yes |
| `AgentSession` creation and reuse | Yes |
| Function tools | Yes |
| Streaming | Yes |
| Service-health evidence retrieval | Yes |
| Deployment evidence retrieval | Yes |
| Runbook guidance retrieval | Yes |
| Missing-field handling | Yes |
| Correlation vs causation handling | Yes |
| Guidance vs evidence separation | Yes |
| Live OpenRouter run | PASS |

---

# Important Limitations

This verification record should not be interpreted as proof that:

- every future package version will behave identically;
- every .NET 10 SDK build is compatible;
- every OpenRouter model will produce the same reasoning;
- every future model response will follow the same wording;
- the sample is a complete production incident-response system;
- the underlying synthetic evidence represents a real production system.

The sample uses fixed synthetic data so that the deterministic layer is
reproducible.

A successful live model run demonstrates observed behavior for the
recorded environment. It does not make probabilistic output deterministic.

---

# Reproducing the Deterministic Verification

From the repository root:

```text
dotnet --version
dotnet build
dotnet test
```

The build and tests require no live model API key.

To inspect resolved application dependencies:

```text
dotnet list src/IncidentTriageAgent/IncidentTriageAgent.csproj package --include-transitive
```

A live run additionally requires:

```text
OPENROUTER_API_KEY
```

and can be started with:

```text
dotnet run --project src/IncidentTriageAgent
```

---

# Related Documents

See:

```text
evidence-first-contract.md
```

for the reasoning and evidence-grounding design contract.

See:

```text
tool-risk-ladder.md
```

for the tool capability and consequence model.

See:

```text
architecture.md
```

for the implementation architecture.

---

## Status

**Reference implementation:** Evidence-First Incident Triage Agent

**Verification date:** 26 September 2026

**Build:** PASS

**Automated tests:** 14/14 PASS

**Live Agent Framework run:** PASS

**Target framework:** .NET 10

**Direct Agent Framework package:** `Microsoft.Agents.AI.OpenAI 1.22.0`