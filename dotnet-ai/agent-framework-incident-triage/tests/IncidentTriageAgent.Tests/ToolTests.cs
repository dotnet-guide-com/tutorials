using IncidentTriageAgent.Evidence;
using IncidentTriageAgent.Guidance;
using IncidentTriageAgent.Tools;
using Microsoft.Extensions.AI;
using Xunit;

namespace IncidentTriageAgent.Tests;

public sealed class ToolTests
{
    [Fact]
    public void ServiceHealthTool_ReturnsEvidence101()
    {
        EvidenceStore store = new();
        ServiceHealthTool tool = new(store);

        string result =
            tool.GetServiceHealth("checkout-api");

        Assert.Contains("E-101", result);
        Assert.Contains("status=degraded", result);
        Assert.Contains("error_rate=18.7%", result);

        Assert.Contains(
            "database_pool_utilization=96%",
            result);
    }


    [Fact]
    public void ServiceHealthTool_RejectsUnknownService()
    {
        EvidenceStore store = new();
        ServiceHealthTool tool = new(store);

        string result =
            tool.GetServiceHealth("payments-api");

        Assert.Contains("NO-EVIDENCE", result);
    }


    [Fact]
    public void DeploymentTool_ReturnsEvidence201()
    {
        EvidenceStore store = new();
        DeploymentTool tool = new(store);

        string result =
            tool.GetRecentDeployment("checkout-api");

        Assert.Contains("E-201", result);
        Assert.Contains("deployment=deploy-1842", result);

        Assert.Contains(
            "previous_deployment=deploy-1839",
            result);
    }


    [Fact]
    public void DeploymentTool_DoesNotInventPreviousDeploymentTimestamp()
    {
        EvidenceStore store = new();
        DeploymentTool tool = new(store);

        string result =
            tool.GetRecentDeployment("checkout-api");

        Assert.DoesNotContain(
            "previous_deployment_completed_at",
            result);
    }


    [Fact]
    public void RunbookTool_ReturnsGuidanceNotEvidence()
    {
        RunbookStore store = new();
        RunbookTool tool = new(store);

        string result =
            tool.GetRunbook("checkout-api");

        Assert.Contains(
            "RB-CHECKOUT-03",
            result);

        Assert.Contains(
            "information_type=runbook_guidance",
            result);

        Assert.Contains(
            "does not establish root cause",
            result);
    }


    [Fact]
    public void AllThreeTools_CreateAgentFunctions()
    {
        EvidenceStore evidenceStore = new();
        RunbookStore runbookStore = new();

        ServiceHealthTool healthTool =
            new(evidenceStore);

        DeploymentTool deploymentTool =
            new(evidenceStore);

        RunbookTool runbookTool =
            new(runbookStore);

        AIFunction healthFunction =
            healthTool.CreateFunction();

        AIFunction deploymentFunction =
            deploymentTool.CreateFunction();

        AIFunction runbookFunction =
            runbookTool.CreateFunction();

        Assert.NotNull(healthFunction);
        Assert.NotNull(deploymentFunction);
        Assert.NotNull(runbookFunction);
    }
}