using System.ComponentModel;
using IncidentTriageAgent.Guidance;
using Microsoft.Extensions.AI;

namespace IncidentTriageAgent.Tools;

/// <summary>
/// Read-only tool that exposes operational reference guidance.
///
/// Runbook content is guidance, not observed incident evidence.
/// </summary>
public sealed class RunbookTool
{
    private readonly RunbookStore _store;

    public RunbookTool(
        RunbookStore store)
    {
        _store = store;
    }


    public AIFunction CreateFunction()
    {
        return AIFunctionFactory.Create(
            GetRunbook,
            name: "get_runbook",
            description:
                "Returns read-only operational runbook guidance for a named service. " +
                "Runbook guidance describes recommended investigation steps; " +
                "it is not evidence that a particular failure actually occurred.");
    }


    [Description(
        "Returns operational runbook guidance for a service. " +
        "Runbook content is reference guidance, not observed incident evidence.")]
    public string GetRunbook(
        [Description("Service name, for example checkout-api.")]
        string serviceName)
    {
        RunbookItem? runbook =
            _store.GetForService(serviceName);

        if (runbook is null)
        {
            return
                $"NO-GUIDANCE | No runbook exists for '{serviceName}'.";
        }

        List<string> lines =
        [
            runbook.Id,
            "information_type=runbook_guidance",
            $"service={runbook.Service}",
            $"title={runbook.Title}",
            "",
            "recommended_checks:"
        ];

        for (int i = 0;
             i < runbook.RecommendedChecks.Length;
             i++)
        {
            lines.Add(
                $"{i + 1}. {runbook.RecommendedChecks[i]}");
        }

        lines.Add("");
        lines.Add("caution:");
        lines.Add(runbook.Caution);

        return string.Join(
            Environment.NewLine,
            lines);
    }
}