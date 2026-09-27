using System.ComponentModel;
using IncidentTriageAgent.Evidence;
using Microsoft.Extensions.AI;

namespace IncidentTriageAgent.Tools;

/// <summary>
/// Read-only tool that exposes observed deployment evidence.
/// </summary>
public sealed class DeploymentTool
{
    private readonly EvidenceStore _store;

    public DeploymentTool(EvidenceStore store)
    {
        _store = store;
    }


    /// <summary>
    /// Creates the Agent Framework function exposed to the model.
    /// </summary>
    public AIFunction CreateFunction()
    {
        return AIFunctionFactory.Create(
            GetRecentDeployment,
            name: "get_recent_deployment",
            description:
                "Returns observed read-only deployment evidence " +
                "for a named service.");
    }


    [Description(
        "Returns observed deployment evidence for a service.")]
    public string GetRecentDeployment(
        [Description("Service name, for example checkout-api.")]
        string serviceName)
    {
        EvidenceItem? evidence =
            _store.GetLatest(
                serviceName,
                "deployment");

        if (evidence is null)
        {
            return
                $"NO-EVIDENCE | No deployment record exists for '{serviceName}'.";
        }

        return $"""
            {evidence.Id}
            evidence_type={evidence.Type}
            service={evidence.Service}
            deployment={evidence.GetFact("deployment")}
            deployment_status={evidence.GetFact("deployment_status")}
            completed_at={evidence.ObservedAt:O}
            previous_deployment={evidence.GetFact("previous_deployment")}
            change_summary={evidence.GetFact("change_summary")}
            rollback_performed={evidence.GetFact("rollback_performed")}
            """;
    }
}