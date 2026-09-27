using System.ComponentModel;
using IncidentTriageAgent.Evidence;
using Microsoft.Extensions.AI;

namespace IncidentTriageAgent.Tools;

/// <summary>
/// Read-only tool that exposes observed service-health evidence.
/// </summary>
public sealed class ServiceHealthTool
{
    private readonly EvidenceStore _store;

    public ServiceHealthTool(EvidenceStore store)
    {
        _store = store;
    }


    /// <summary>
    /// Creates the Agent Framework function exposed to the model.
    /// </summary>
    public AIFunction CreateFunction()
    {
        return AIFunctionFactory.Create(
            GetServiceHealth,
            name: "get_service_health",
            description:
                "Returns observed read-only operational health evidence " +
                "for a named service.");
    }


    [Description(
        "Returns observed operational health evidence for a service.")]
    public string GetServiceHealth(
        [Description("Service name, for example checkout-api.")]
        string serviceName)
    {
        EvidenceItem? evidence =
            _store.GetLatest(
                serviceName,
                "service_health");

        if (evidence is null)
        {
            return
                $"NO-EVIDENCE | No service health record exists for '{serviceName}'.";
        }

        return $"""
            {evidence.Id}
            evidence_type={evidence.Type}
            service={evidence.Service}
            observed_at={evidence.ObservedAt:O}
            status={evidence.GetFact("status")}
            error_rate={evidence.GetFact("error_rate")}
            error_spike_started={evidence.GetFact("error_spike_started")}
            database_pool_utilization={evidence.GetFact("database_pool_utilization")}
            """;
    }
}