\# Evidence-First Incident Triage Agent — Verified Sample Run



This document records one successful live execution of the

\*\*Evidence-First Incident Triage Agent\*\* reference sample.



The run demonstrates:



\- Microsoft Agent Framework agent creation;

\- read-only function-tool use;

\- deterministic operational evidence retrieval;

\- runbook guidance retrieval;

\- `AgentSession` reuse;

\- streaming output;

\- missing-field handling;

\- evidence-versus-inference separation;

\- correlation-versus-causation handling.



> This is a captured example, not a golden-output test.

> Model-generated wording is probabilistic and may differ between runs.



\---



\# Verification Date



\*\*26 September 2026\*\*



\---



\# Verified Environment



The captured run used:



```text

Target framework: net10.0

.NET SDK:         10.0.401

Agent package:    Microsoft.Agents.AI.OpenAI 1.22.0

Provider:         OpenRouter

Model:            openai/gpt-4o-mini

```



See:



```text

verified-environment.md

```



for the complete verification record.



\---



\# Scenario



The sample simulates an incident involving:



```text

checkout-api

```



The agent can retrieve information from three read-only tools:



```text

get\_service\_health

get\_recent\_deployment

get\_runbook

```



These tools expose deterministic evidence and reference guidance stored

with the repository.



\---



\# Deterministic Evidence Available to the Agent



\## Service-health evidence



```text

E-101

service=checkout-api

observed\_at=2026-09-24T14:12:00Z

status=degraded

error\_rate=18.7%

error\_spike\_started=2026-09-24T14:09:00Z

database\_pool\_utilization=96%

```



\## Deployment evidence



```text

E-201

service=checkout-api

deployment=deploy-1842

deployment\_status=succeeded

completed\_at=2026-09-24T14:02:00Z

previous\_deployment=deploy-1839

change\_summary=pricing-rule refresh and structured logging update

rollback\_performed=false

```



Notice that the evidence does \*\*not\*\* contain:



```text

previous\_deployment\_completed\_at

```



for:



```text

deploy-1839

```



That omission is intentional.



The agent should leave the missing timestamp unknown rather than invent

one.



\---



\# Reference Guidance



The runbook tool exposes:



```text

RB-CHECKOUT-03

```



with investigation guidance including:



```text

Compare error-rate onset with recent deployments.

Inspect database connection-pool utilization.

Check database connection counts before and after deployment.

Review traces for timeout or connection-acquisition failures.

Compare configuration with the previous known-good release.

```



This is \*\*reference guidance\*\*, not observed incident evidence.



\---



\# Initial Prompt



The application sends the following incident request:



```text

Investigate the checkout-api incident.



Using only the available operational evidence and reference guidance:



1\. state what the observed evidence establishes,

2\. identify reasonable inferences,

3\. list the relevant runbook guidance,

4\. identify unknown information,

5\. and state which root-cause claims remain unproven.



Do not invent missing facts.

Do not treat correlation as proof of causation.

```



\---



\# Captured Live Output



The following output was observed during the verified live run:



```text

============================================

&#x20;Evidence-First Incident Triage Agent

============================================



Model: openai/gpt-4o-mini

Agent: ChatClientAgent

Session: ChatClientAgentSession



=== INCIDENT ASSESSMENT ===



\### OBSERVED EVIDENCE

1\. The \*\*checkout-api\*\* service has a status of \*\*degraded\*\* with an \*\*error rate of 18.7%\*\* observed on September 24, 2026, at 14:12 UTC (Evidence ID: E-101).

2\. A \*\*deployment labeled deploy-1842\*\* was completed successfully on September 24, 2026, at 14:02 UTC, with a previous deployment labeled \*\*deploy-1839\*\* (Evidence ID: E-201).

3\. The \*\*error spike\*\* began at \*\*14:09 UTC\*\*, which is after the completion of the last deployment (Evidence ID: E-101).

4\. The \*\*database pool utilization\*\* was at \*\*96%\*\* (Evidence ID: E-101).



\### REASONABLE INFERENCES

1\. The error rate increased after the most recent deployment (deploy-1842), suggesting a potential connection that requires further investigation.

2\. The high database pool utilization (96%) may be contributing to the degraded service status and high error rate.

3\. The spike in errors beginning shortly after the latest deployment may indicate issues that arose as a result of changes made in that deployment.



\### RUNBOOK GUIDANCE

The relevant runbook guidance (RB-CHECKOUT-03) suggests the following checks:

1\. Compare the onset of the error rate with recent deployments.

2\. Inspect database connection-pool utilization.

3\. Check database connection counts before and after the deployment.

4\. Review traces for timeout or connection-acquisition failures.

5\. Compare the current configuration with the previous known-good release.



It is noted that the presence of these checks does not establish root cause.



\### UNKNOWNS

1\. The timestamp of the previous deployment (deploy-1839) is UNKNOWN.

2\. The specific reasons for the increase in error rate remain UNKNOWN, as no root cause has been identified.

3\. The connection counts for the database before and after the deployment are UNKNOWN.



\### UNPROVEN CLAIMS

1\. There is no established root cause for the degraded status and increased error rate, as the current evidence does not support a definitive conclusion regarding causation between the deployment and the errors.

2\. It remains unproven whether the high database pool utilization is solely responsible for the degraded status.

```



\---



\# What the Initial Run Demonstrated



Several behaviors are important.



\## 1. Evidence IDs were preserved



The generated assessment referred to:



```text

E-101

E-201

RB-CHECKOUT-03

```



This makes factual claims easier to trace back to deterministic sources.



\---



\## 2. The missing deployment timestamp stayed unknown



The agent stated:



```text

The timestamp of the previous deployment (deploy-1839) is UNKNOWN.

```



This is important because the evidence contains the identifier:



```text

previous\_deployment=deploy-1839

```



but contains no corresponding timestamp.



The model did not fill that missing field with a plausible value.



That behavior demonstrates Rule 2 of the

\*\*Evidence-First Agent Contract\*\*:



> Never fill a missing evidence field.



\---



\## 3. Runbook guidance remained guidance



The agent retrieved:



```text

RB-CHECKOUT-03

```



but did not claim that the runbook itself proved a root cause.



It explicitly stated:



```text

the presence of these checks does not establish root cause

```



This demonstrates the intended distinction:



```text

guidance

&#x20;   ≠

evidence

```



\---



\## 4. High database utilization was treated as an inference



The observed fact was:



```text

database\_pool\_utilization=96%

```



The generated interpretation used qualified wording:



```text

may be contributing

```



rather than claiming that database pool pressure was definitively the

root cause.



\---



\# A Subtle Probabilistic Reasoning Issue



One generated sentence was:



```text

The spike in errors beginning shortly after the latest deployment may

indicate issues that arose as a result of changes made in that deployment.

```



This is still qualified with:



```text

may

```



and the later `UNPROVEN CLAIMS` section explicitly rejects a definitive

causal conclusion.



However, this sentence shows why prompt instructions alone should not be

treated as a mathematical guarantee.



A model may occasionally phrase an inference more strongly than an

engineer would prefer.



The deterministic evidence layer remains unchanged:



```text

deployment completed: 14:02 UTC

error spike began:     14:09 UTC

```



Those facts establish chronology.



They do not establish causation.



This is one reason the repository separates:



```text

deterministic evidence

&#x20;       ↓

probabilistic interpretation

```



and does not make exact live-model wording a normal CI assertion.



\---



\# Same-Session Follow-Up



After the initial assessment, the application reused the same:



```text

AgentSession

```



and sent this follow-up:



```text

Can I truthfully write in the incident report:



"deploy-1842 caused the checkout-api outage"?



Give a concise evidence-based answer.

```



\---



\# Captured Same-Session Response



The verified live response was:



```text

No, you cannot truthfully write that "deploy-1842 caused the checkout-api outage." The evidence indicates a correlation between the deployment and the increase in error rate, but correlation does not establish causation. The specific root cause of the outage has not been confirmed, and the evidence does not definitively support the claim that the deployment was the cause of the outage.

```



\---



\# Why the Follow-Up Matters



The evidence establishes this sequence:



```text

14:02 UTC

deploy-1842 completed



&#x20;       ↓ 7 minutes



14:09 UTC

checkout-api error spike began

```



The chronology makes the deployment a reasonable investigation target.



It does not prove:



```text

deploy-1842

&#x20;     ↓

caused

&#x20;     ↓

checkout-api outage

```



The same-session response correctly preserved the distinction:



```text

correlation

&#x20;   ≠

causation

```



This is one of the central behaviors of the reference sample.



\---



\# Session Reuse



The application creates one:



```text

AgentSession

```



and reuses it for:



```text

incident assessment

&#x20;       ↓

follow-up question

```



The second prompt does not restate:



```text

E-101

E-201

RB-CHECKOUT-03

```



yet the agent can answer in the context of the incident already discussed

within the session.



This demonstrates conversational session continuity without adding a

separate persistent-memory system.



\---



\# What This Run Does Not Prove



A successful live run does not prove that:



\- every future invocation will produce identical wording;

\- every model will select tools in exactly the same order;

\- every model will make equally cautious inferences;

\- prompt instructions eliminate hallucination;

\- tool access alone guarantees grounded reasoning;

\- the sample is ready for unsupervised production remediation.



The captured run demonstrates observed behavior in the verified

environment.



The deterministic components are covered separately by automated tests.



\---



\# Automated Verification Boundary



The repository uses automated tests for behavior that should be

deterministic.



At the time of this run:



```text

total: 14

failed: 0

succeeded: 14

skipped: 0

```



The automated suite verifies areas such as:



```text

evidence loading

runbook loading

service lookup

missing-field behavior

tool output

unknown-service behavior

AIFunction construction

```



The test suite requires no model-provider API key.



Live model reasoning is verified separately.



\---



\# Evidence-First Interpretation of the Incident



The safest summary of the sample incident is:



```text

KNOWN

\-----

checkout-api was degraded

error rate was 18.7%

database pool utilization was 96%

deploy-1842 completed at 14:02 UTC

the error spike began at 14:09 UTC

deploy-1839 was the previous deployment



REASONABLE TO INVESTIGATE

\-------------------------

deploy-1842

database connection-pool pressure



UNKNOWN

\-------

deploy-1839 completion time

database connection counts before and after deployment

trace evidence for timeout or connection-acquisition failures

confirmed root cause



UNPROVEN

\--------

deploy-1842 caused the outage

database pool pressure caused the outage

```



That separation is the core behavior this sample is designed to teach.



\---



\# Related Documents



See:



```text

evidence-first-contract.md

```



for the five evidence-first reasoning rules.



See:



```text

tool-risk-ladder.md

```



for the classification of read-only and higher-risk agent tools.



See:



```text

architecture.md

```



for the implementation architecture.



See:



```text

verified-environment.md

```



for package versions, SDK information, tests, and the complete

verification record.



\---



\## Status



\*\*Sample:\*\* Evidence-First Incident Triage Agent



\*\*Run type:\*\* controlled live verification



\*\*Provider:\*\* OpenRouter



\*\*Model:\*\* `openai/gpt-4o-mini`



\*\*Agent:\*\* `ChatClientAgent`



\*\*Session:\*\* `ChatClientAgentSession`



\*\*Deterministic tests at verification:\*\* 14/14 passing



\*\*Live run:\*\* PASS



\*\*Last verified:\*\* 26 September 2026

