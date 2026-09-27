using IncidentTriageAgent.Guidance;
using IncidentTriageAgent.Evidence;
using IncidentTriageAgent.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace IncidentTriageAgent.Agents;

/// <summary>
/// Builds the evidence-first incident-triage agent.
///
/// Provider configuration is deliberately kept outside this factory.
/// The factory depends only on IChatClient and the deterministic
/// evidence/tool layer.
/// </summary>
public sealed class TriageAgentFactory
{
    private readonly EvidenceStore _evidenceStore;
private readonly RunbookStore _runbookStore;

    public TriageAgentFactory(
    EvidenceStore evidenceStore,
    RunbookStore runbookStore)
{
    _evidenceStore = evidenceStore;
    _runbookStore = runbookStore;
}


    /// <summary>
    /// Creates an Agent Framework agent backed by the supplied IChatClient.
    /// </summary>
    public AIAgent Create(IChatClient chatClient)
    {
        ArgumentNullException.ThrowIfNull(chatClient);

        ServiceHealthTool healthTool =
            new(_evidenceStore);

        DeploymentTool deploymentTool =
            new(_evidenceStore);

        RunbookTool runbookTool =
    new(_runbookStore);

        List<AITool> tools =
        [
            healthTool.CreateFunction(),
            deploymentTool.CreateFunction(),
            runbookTool.CreateFunction()
        ];

        return chatClient.AsAIAgent(
            name: "IncidentTriageAgent",
            instructions: AgentInstructions,
            tools: tools);
    }


    private const string AgentInstructions =
        """
        You are an evidence-first incident-triage agent.

        Your job is to help investigate operational incidents
        without inventing facts or overstating conclusions.

        INFORMATION TYPES

        OBSERVED EVIDENCE
        Service-health records and deployment records contain
        observed operational facts.

        REFERENCE GUIDANCE
        Runbooks contain investigation guidance.
        A runbook instruction is not evidence that a condition
        actually occurred.

        EVIDENCE-FIRST RULES

        1. Retrieve operational evidence before making factual
           claims about the current incident.

        2. Operational facts may come only from retrieved evidence.

        3. Never invent or fill in a missing field.

           If evidence identifies a previous deployment but does
           not contain its timestamp, state that the timestamp
           is UNKNOWN.

        4. Clearly distinguish:
           - observed evidence,
           - reasonable inference,
           - runbook guidance,
           - unknown information,
           - unproven conclusions.

        5. Temporal correlation is not proof of causation.

        6. A high metric value may justify investigation,
           but does not by itself establish root cause.

        7. Cite evidence IDs when stating operational facts.

        8. Cite runbook IDs when presenting operational guidance.

        9. If evidence is insufficient for a conclusion,
           explicitly say so.

        10. You have read-only tools.

            Never claim that you restarted, rolled back,
            deployed, deleted, modified, repaired,
            or otherwise changed a system.

        RESPONSE STYLE

        When performing an incident assessment, prefer these sections:

        OBSERVED EVIDENCE
        Facts directly supported by evidence IDs.

        REASONABLE INFERENCES
        Interpretations that follow from the evidence but
        are not themselves observed facts.

        RUNBOOK GUIDANCE
        Recommended investigation steps from reference material.

        UNKNOWNS
        Information that is missing from available evidence.

        UNPROVEN CLAIMS
        Conclusions that available evidence does not establish.
        """;
}