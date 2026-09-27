# AI Agent Tool Risk Ladder

The **AI Agent Tool Risk Ladder** is a practical way to classify tools
based on what they are allowed to do and the potential impact of an
incorrect or unauthorized tool call.

It was created for the
**Evidence-First Incident Triage Agent** reference sample from
dotnet-guide.com.

This is not an official Microsoft Agent Framework classification.
It is an application-level design model that can be adapted to other
tool-using AI systems.

---

# Why classify agent tools?

Not all tools carry the same risk.

Consider these two functions:

```text
get_service_health()
restart_service()
```

Both might be callable by an AI agent.

But their consequences are very different.

The first reads information.

The second changes a running system.

Treating both as equivalent "tools" hides an important security and
operational distinction.

The Tool Risk Ladder makes that distinction explicit.

---

# Level 0 — Reference

## Purpose

Returns static or reference information.

Examples:

```text
read_runbook
get_documentation
lookup_error_code
read_policy
```

Typical characteristics:

- no system-state changes;
- normally low operational impact;
- output may still contain incorrect or outdated information;
- source quality still matters.

Example from this repository:

```text
get_runbook
```

The tool returns:

```text
RB-CHECKOUT-03
```

which contains diagnostic guidance.

The agent can read it, but nothing is changed.

### Typical controls

- validate tool inputs;
- restrict accessible resources;
- identify the source;
- distinguish reference guidance from observed evidence.

---

# Level 1 — Observational

## Purpose

Reads current or historical system state.

Examples:

```text
get_service_health
get_recent_deployment
get_database_metrics
read_logs
query_traces
```

Example from this repository:

```text
get_service_health
get_recent_deployment
```

These tools expose evidence such as:

```text
E-101
error_rate=18.7%
database_pool_utilization=96%
```

and:

```text
E-201
deployment=deploy-1842
```

The tool still does not modify the system.

However, Level 1 can be more sensitive than Level 0 because operational
data may include confidential or security-relevant information.

### Typical controls

- authentication;
- authorization;
- input validation;
- data minimization;
- audit logging;
- evidence/source identifiers;
- access restrictions for sensitive telemetry.

---

# Level 2 — Reversible Write

## Purpose

Changes system state in a way that is normally easy to review or undo.

Examples:

```text
create_incident_note
add_ticket_comment
set_case_label
create_draft
update_noncritical_metadata
```

The agent is no longer simply observing.

It is creating or modifying information.

Although the action may be reversible, incorrect tool calls can create
confusion, unwanted records, or workflow problems.

### Typical controls

In addition to Level 0 and Level 1 controls:

- validate proposed changes;
- show the intended action before execution where appropriate;
- enforce user or service permissions;
- record who or what initiated the action;
- support rollback or correction.

---

# Level 3 — Operational Action

## Purpose

Changes a running system or operational environment.

Examples:

```text
restart_service
scale_application
disable_feature
invalidate_cache
rollback_deployment
rotate_runtime_configuration
```

An incorrect call can cause downtime or materially change production
behavior.

A model recommendation and authority to execute that recommendation
should be treated as separate decisions.

For example:

```text
Agent conclusion:
"Rolling back deploy-1842 may be worth considering."

does not imply:

Agent permission:
rollback_deployment("deploy-1842")
```

### Typical controls

Level 3 tools should normally introduce additional safeguards such as:

- explicit authorization;
- bounded parameters;
- environment restrictions;
- human approval for consequential actions;
- idempotency where possible;
- audit logs;
- dry-run or preview modes;
- rollback planning;
- rate limits.

---

# Level 4 — Destructive or High-Impact External Action

## Purpose

Performs actions that may be difficult to reverse, affect external
parties, or create substantial operational consequences.

Examples:

```text
delete_production_data
drop_database
deploy_to_all_regions
send_customer_notification
revoke_all_credentials
terminate_resource
execute_financial_transaction
```

These tools should not be treated as ordinary extensions of model
reasoning.

The consequence of an incorrect invocation can be much larger than the
cost of an incorrect model response.

### Typical controls

Possible controls include:

- strong authentication;
- narrow authorization;
- explicit human approval;
- multi-party approval for high-impact operations;
- deterministic validation outside the model;
- transaction limits;
- environment restrictions;
- immutable audit logs;
- rollback or recovery mechanisms where possible;
- policy enforcement independent of the LLM.

Some Level 4 operations may be inappropriate for autonomous execution
altogether.

---

# The Risk Ladder

A simplified view is:

```text
Level 0
REFERENCE
Read runbook
        │
        ▼
Level 1
OBSERVATIONAL
Read service health
        │
        ▼
Level 2
REVERSIBLE WRITE
Create incident note
        │
        ▼
Level 3
OPERATIONAL ACTION
Restart service
        │
        ▼
Level 4
DESTRUCTIVE / HIGH IMPACT
Delete data or perform irreversible external action
```

As the level increases:

```text
potential consequence ↑
authorization needs   ↑
validation needs      ↑
approval requirements ↑
audit requirements    ↑
```

---

# Why the reference agent stops at Levels 0 and 1

The Evidence-First Incident Triage Agent intentionally exposes only:

```text
get_runbook             Level 0
get_service_health      Level 1
get_recent_deployment   Level 1
```

It can investigate.

It cannot remediate.

This is deliberate.

The first tutorial focuses on:

```text
retrieve
↓
evaluate
↓
explain
```

rather than:

```text
retrieve
↓
decide
↓
change production
```

This keeps the core Agent Framework example understandable while also
providing a safer boundary for a first tool-using agent.

---

# Recommendation Is Not Authorization

One of the most important distinctions in agent design is:

```text
model recommends an action
        ≠
system authorizes the action
```

For example, an agent may reasonably conclude:

> Consider rolling back deploy-1842 if additional evidence confirms
> that the deployment introduced the failure.

That conclusion should not automatically grant access to:

```text
rollback_deployment()
```

Authorization should be enforced outside the language model.

---

# Tool Risk Can Depend on Context

A tool does not necessarily have one universal risk level.

For example:

```text
restart_service
```

might be:

- relatively low risk in a developer sandbox;
- high risk in a production payment system.

Likewise:

```text
create_ticket
```

may be low impact internally but more consequential if it automatically
triggers an external workflow.

The ladder should therefore be treated as a classification aid rather
than an absolute universal rating.

---

# Design Questions for Every Tool

Before exposing a function to an agent, ask:

1. Does the tool only read information or can it change state?
2. Can the action be reversed?
3. What happens if the model calls it with the wrong arguments?
4. What happens if the model calls it repeatedly?
5. Does the user or service actually have permission to perform it?
6. Should a human approve execution?
7. Can deterministic code validate the request before execution?
8. Is the action recorded in an audit log?
9. Can its scope be reduced?
10. Is exposing this operation to an AI agent necessary at all?

---

# Relationship to the Evidence-First Agent Contract

The two patterns address different concerns.

The **Evidence-First Agent Contract** asks:

> Is the agent making claims that are actually supported by retrieved
> information?

The **Tool Risk Ladder** asks:

> What is the potential consequence if the agent invokes this tool?

Together:

```text
Evidence-First Contract
        ↓
controls claims and reasoning

Tool Risk Ladder
        ↓
controls capability and consequence
```

A well-grounded agent can still be dangerous if it has excessive tool
permissions.

Likewise, a read-only agent can still produce poor conclusions if it
misrepresents evidence.

Both boundaries matter.

---

# Reference Implementation

The accompanying .NET reference sample currently uses only Level 0 and
Level 1 tools:

| Tool | Level | Capability |
|---|---|---|
| `get_runbook` | 0 | Reference guidance |
| `get_service_health` | 1 | Read operational health evidence |
| `get_recent_deployment` | 1 | Read deployment evidence |

There are intentionally no write-capable or destructive tools.

Future examples may demonstrate stronger controls around higher-risk
tools, including explicit approval boundaries.

---

## Status

**Pattern status:** tutorial/reference design model

**Specification status:** not an official Microsoft specification

**Reference implementation:** Evidence-First Incident Triage Agent

**Last verified:** 26 September 2026