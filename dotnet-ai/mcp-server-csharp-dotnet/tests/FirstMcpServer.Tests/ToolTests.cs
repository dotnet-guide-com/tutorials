using FirstMcpServer.Models;
using FirstMcpServer.Stores;
using FirstMcpServer.Tools;

namespace FirstMcpServer.Tests;

public sealed class ToolTests
{
    [Fact]
    public void ServiceHealthTool_ReturnsEvidence()
    {
        var store = ServiceHealthStore.FromRecords(
        [
            new ServiceHealthRecord
            {
                EvidenceId = "E-101",
                Service = "checkout-api",
                ObservedAt = "2026-09-24T14:12:00Z",
                Status = "degraded",
                ErrorRate = 18.7,
                ErrorSpikeStarted = "2026-09-24T14:09:00Z",
                DatabasePoolUtilization = 96
            }
        ]);

        string result = ServiceHealthTools.GetServiceHealth(store, "checkout-api");

        Assert.Contains("\"evidenceId\":\"E-101\"", result);
        Assert.Contains("\"status\":\"degraded\"", result);
    }

    [Fact]
    public void DeploymentTool_ReturnsEvidence()
    {
        var store = DeploymentStore.FromRecords(
        [
            new DeploymentRecord
            {
                EvidenceId = "E-201",
                Service = "checkout-api",
                Deployment = "deploy-1842",
                DeploymentStatus = "succeeded",
                CompletedAt = "2026-09-24T14:02:00Z",
                PreviousDeployment = "deploy-1839",
                ChangeSummary = "pricing-rule refresh",
                RollbackPerformed = false
            }
        ]);

        string result = DeploymentTools.GetRecentDeployment(store, "checkout-api");

        Assert.Contains("\"evidenceId\":\"E-201\"", result);
        Assert.Contains("\"previousDeployment\":\"deploy-1839\"", result);
    }

    [Fact]
    public void RunbookTool_ReturnsGuidance()
    {
        var store = RunbookStore.FromRecords(
        [
            new RunbookRecord
            {
                RunbookId = "RB-CHECKOUT-03",
                Service = "checkout-api",
                Title = "Checkout API degraded-service investigation",
                Guidance = ["Inspect database connection-pool utilization."],
                Caution = "Guidance does not establish root cause."
            }
        ]);

        string result = RunbookTools.GetRunbook(store, "checkout-api");

        Assert.Contains("\"runbookId\":\"RB-CHECKOUT-03\"", result);
        Assert.Contains("does not establish root cause", result);
    }

    [Fact]
    public void Tool_ReturnsFoundFalseForUnknownService()
    {
        var store = ServiceHealthStore.FromRecords([]);

        string result = ServiceHealthTools.GetServiceHealth(store, "missing-api");

        Assert.Contains("\"found\":false", result);
        Assert.Contains("\"service\":\"missing-api\"", result);
    }
}
