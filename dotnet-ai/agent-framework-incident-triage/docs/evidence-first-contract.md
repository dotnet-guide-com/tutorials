# Evidence-First Agent Contract

The **Evidence-First Agent Contract** is a practical design pattern for
tool-using AI agents that need to reason about operational facts without
silently inventing missing information.

It was created for the
**Evidence-First Incident Triage Agent** reference sample from
dotnet-guide.com.

This is **not a Microsoft Agent Framework specification** and is not a
replacement for framework documentation. It is an application-level
design contract that can be implemented with Microsoft Agent Framework
or adapted to other tool-using agent systems.

---

# Why this contract exists

Giving an AI agent access to tools does not automatically make its
answers evidence-grounded.

An agent can:

1. retrieve correct data from a tool;
2. accurately repeat some of that data;
3. and still introduce unsupported facts while connecting the pieces.

For example, suppose a deployment tool returns:

```text
E-201
deployment=deploy-1842
completed_at=2026-09-24T14:02:00Z
previous_deployment=deploy-1839
```

The evidence establishes that:

- `deploy-1842` completed at 14:02 UTC;
- `deploy-1839` was the previous deployment.

It does **not** establish when `deploy-1839` occurred.

An evidence-first agent should therefore say:

> The completion time of deploy-1839 is unknown from the available evidence.

It should not fill the missing field with a plausible time.

This distinction is the foundation of the contract.

---

# The Five Evidence-First Rules

## Rule 1 — Retrieve before asserting

Claims about current system state should originate from an appropriate
evidence source.

If the agent needs to state:

> checkout-api has an 18.7% error rate

that value should come from a retrieved evidence record rather than from
model memory, assumption, or prompt context that was never verified.

In the reference sample:

```text
get_service_health
        ↓
E-101
        ↓
error_rate=18.7%
```

The model may explain the value, but the value itself belongs to the
evidence layer.

---

## Rule 2 — Never fill a missing evidence field

Missing information must remain missing.

If the evidence contains:

```text
previous_deployment=deploy-1839
```

but no timestamp, the agent may state:

> deploy-1839 was the previous deployment.

It must not state:

> deploy-1839 occurred shortly before deploy-1842.

unless another evidence source actually establishes that timing.

A useful implementation rule is:

```text
missing field
    ↓
UNKNOWN
```

not:

```text
missing field
    ↓
model-generated completion
```

This applies even when the inferred value would appear highly plausible.

---

## Rule 3 — Separate evidence from inference

Observed facts and model interpretation should not be presented as if
they have the same evidentiary status.

Consider:

```text
E-101
database_pool_utilization=96%
```

The following is an observed fact:

> Database pool utilization was 96%.

The following is an inference:

> High pool utilization may indicate resource pressure.

The following would be an unsupported causal claim:

> Database pool exhaustion caused the outage.

An evidence-first response should make these levels distinguishable.

A practical response structure is:

```text
OBSERVED EVIDENCE
REASONABLE INFERENCES
RUNBOOK GUIDANCE
UNKNOWNS
UNPROVEN CLAIMS
```

---

## Rule 4 — Correlation is not causation

Timing can make an event worth investigating without proving that the
event caused the incident.

The sample contains:

```text
deploy-1842 completed        14:02 UTC
checkout-api error spike     14:09 UTC
```

This establishes a seven-minute temporal relationship.

It supports the statement:

> deploy-1842 is a reasonable investigation target.

It does not by itself support:

> deploy-1842 caused the outage.

Additional evidence could include:

- traces showing failures introduced by changed code;
- connection counts before and after deployment;
- configuration differences;
- rollback results;
- comparison with the previous known-good release;
- dependency or database telemetry;
- reproducible behavior tied to the deployed change.

An evidence-first agent should preserve this distinction explicitly.

---

## Rule 5 — Guidance is not evidence

Reference material can tell an agent what to investigate without proving
that a particular condition exists.

For example, the sample runbook contains:

```text
RB-CHECKOUT-03

Inspect database connection-pool utilization.
Review traces for timeout or connection-acquisition failures.
Compare configuration with the previous known-good release.
```

Those instructions are **guidance**.

They do not establish that:

- a connection pool was exhausted;
- a timeout occurred;
- the configuration changed incorrectly.

The runbook helps determine the next investigation step.

The evidence layer determines what is currently known.

---

# Evidence Categories

The reference sample intentionally separates information into different
categories.

## Observed evidence

Examples:

```text
E-101
E-201
```

Observed evidence represents deterministic operational records such as:

- service health;
- error rates;
- deployment identifiers;
- timestamps;
- resource utilization.

These records should be traceable to their source.

---

## Reference guidance

Example:

```text
RB-CHECKOUT-03
```

Reference guidance represents material such as:

- runbooks;
- troubleshooting procedures;
- operational checklists;
- recommended diagnostic steps.

Guidance can help determine what to inspect next.

It should not be treated as proof that a condition occurred.

---

## Inference

Inference is the agent's interpretation of retrieved information.

For example:

> 96% database pool utilization suggests resource pressure.

This may be useful and reasonable, but it is still an interpretation.

Inference should be presented as such.

---

## Unknown

An unknown is information that the available evidence does not contain.

For example:

```text
previous_deployment=deploy-1839
```

does not provide:

```text
previous_deployment_completed_at
```

Therefore the completion time is:

```text
UNKNOWN
```

Unknowns are an expected and useful output of an evidence-first system.

---

## Unproven claim

An unproven claim is a conclusion that available evidence does not
establish.

For example:

> deploy-1842 caused the checkout-api outage.

The reference evidence shows temporal correlation but does not establish
that causal relationship.

The appropriate output is therefore:

```text
UNPROVEN
```

rather than a confident yes.

---

# Evidence IDs

Where practical, evidence-first systems should attach stable identifiers
to retrieved records.

The sample uses identifiers such as:

```text
E-101
E-201
RB-CHECKOUT-03
```

This provides several benefits:

- users can see where a factual statement came from;
- tests can verify stable evidence;
- debugging becomes easier;
- model output can be compared with the retrieved source;
- downstream systems can trace claims back to records.

An evidence ID does not make the underlying information correct by
itself. It provides traceability.

---

# Deterministic Evidence vs Probabilistic Interpretation

One of the most important boundaries in an agent system is:

```text
deterministic evidence
        ↓
probabilistic interpretation
```

The sample's evidence layer is deterministic.

For example:

```text
E-101
error_rate=18.7%
database_pool_utilization=96%
```

Automated tests can verify those values exactly.

The model's interpretation may vary between runs:

> 96% utilization may indicate resource pressure.

Another run might phrase this differently.

Therefore:

- evidence retrieval should be tested deterministically;
- tool contracts should be tested deterministically;
- missing-field behavior should be tested deterministically;
- model-generated wording should not normally be treated as a stable CI assertion.

A successful live model run demonstrates observed behavior.

It does not guarantee that every future model invocation will produce
identical reasoning or wording.

---

# Read-Only First

The reference implementation intentionally gives the first agent only
read-only tools.

It can:

```text
read service health
read deployment information
read runbook guidance
```

It cannot:

```text
restart a service
roll back a deployment
modify configuration
delete data
deploy code
```

This separates:

```text
investigation
```

from:

```text
action
```

An agent that can recommend an action is not automatically authorized to
perform that action.

Write-capable and destructive tools require additional controls such as
explicit approval, authorization, validation, audit logging, and
appropriate operational safeguards.

These controls are outside the scope of the first reference sample.

---

# Example Evidence-First Assessment

Given:

```text
E-101
service=checkout-api
status=degraded
error_rate=18.7%
error_spike_started=2026-09-24T14:09:00Z
database_pool_utilization=96%
```

and:

```text
E-201
deployment=deploy-1842
completed_at=2026-09-24T14:02:00Z
previous_deployment=deploy-1839
```

an evidence-first assessment could state:

### Observed evidence

- E-101 shows that checkout-api is degraded.
- E-101 reports an error rate of 18.7%.
- E-101 reports database pool utilization of 96%.
- E-201 shows that deploy-1842 completed at 14:02 UTC.
- E-101 shows that the error spike began at 14:09 UTC.

### Reasonable inference

The timing makes deploy-1842 a reasonable investigation target.

The 96% pool utilization makes resource pressure another reasonable
investigation lead.

### Unknown

The completion time of deploy-1839 is not available.

### Unproven

The evidence does not establish that deploy-1842 caused the outage.

The evidence does not establish that database pool pressure caused the
outage.

This is the behavior the contract is designed to encourage.

---

# Anti-Patterns

An agent violates the Evidence-First Agent Contract when it does things
such as:

## Inventing missing facts

```text
Evidence:
previous_deployment=deploy-1839

Agent:
deploy-1839 completed shortly before deploy-1842
```

No timing evidence exists.

---

## Turning guidance into fact

```text
Runbook:
Inspect connection-pool exhaustion.

Agent:
The connection pool was exhausted.
```

The runbook is a diagnostic instruction, not incident evidence.

---

## Turning correlation into causation

```text
Deployment completed: 14:02
Error spike began:     14:09

Agent:
The deployment caused the outage.
```

The chronology supports investigation, not proof of causation.

---

## Hiding uncertainty

```text
Evidence is insufficient.

Agent:
The most likely root cause is definitely...
```

An evidence-first agent should expose important uncertainty rather than
mask it with confident language.

---

## Claiming actions that were never executed

```text
Available tools:
read-only

Agent:
I rolled back the deployment.
```

The model must not claim an operational action that no tool performed.

---

# Implementation Checklist

An implementation following this pattern should aim to answer yes to
these questions:

- Does every operational fact have a retrievable source?
- Can missing fields remain explicitly unknown?
- Are evidence and guidance represented separately?
- Are inference and fact distinguishable in the response?
- Are evidence records traceable with stable IDs?
- Are deterministic components testable without an LLM?
- Can the system build and test without production secrets?
- Are read-only and write-capable tools clearly separated?
- Does the system avoid claiming actions that were not executed?
- Are causal conclusions withheld when evidence establishes only
  correlation?

---

# Scope

The Evidence-First Agent Contract is intentionally narrow.

It does not solve:

- factual correctness of the underlying data source;
- authentication or authorization;
- malicious or compromised tools;
- prompt injection;
- model evaluation;
- long-term memory;
- multi-agent coordination;
- human approval workflows;
- production observability;
- regulatory or compliance requirements.

Those concerns require additional controls.

The contract addresses one specific problem:

> **How should a tool-using agent distinguish retrieved facts,
> interpretation, guidance, missing information, and unproven claims?**

---

# Reference Implementation

The accompanying .NET sample demonstrates this contract using:

- .NET 10;
- Microsoft Agent Framework;
- `IChatClient`;
- `AIAgent`;
- `AgentSession`;
- read-only function tools;
- deterministic JSON evidence;
- deterministic automated tests;
- a live model only for the probabilistic reasoning layer.

See `verified-environment.md` for the exact environment in which the
reference implementation was built and tested.

---

## Status

**Pattern status:** tutorial/reference design pattern

**Specification status:** not an official Microsoft specification

**Reference implementation:** Evidence-First Incident Triage Agent

**Last verified:** 26 September 2026