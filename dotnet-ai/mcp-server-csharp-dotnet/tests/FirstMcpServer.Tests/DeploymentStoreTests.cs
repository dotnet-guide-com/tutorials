using FirstMcpServer.Models;
using FirstMcpServer.Stores;

namespace FirstMcpServer.Tests;

public sealed class DeploymentStoreTests
{
    private static DeploymentStore CreateStore()
        => DeploymentStore.FromRecords(
        [
            new DeploymentRecord
            {
                EvidenceId = "E-201",
                Service = "checkout-api",
                Deployment = "deploy-1842",
                DeploymentStatus = "succeeded",
                CompletedAt = "2026-09-24T14:02:00Z",
                PreviousDeployment = "deploy-1839",
                ChangeSummary = "pricing-rule refresh and structured logging update",
                RollbackPerformed = false
            }
        ]);

    [Fact]
    public void GetRecent_ReturnsKnownDeployment()
    {
        var record = CreateStore().GetRecent("checkout-api");

        Assert.NotNull(record);
        Assert.Equal("E-201", record.EvidenceId);
        Assert.Equal("deploy-1842", record.Deployment);
        Assert.Equal("deploy-1839", record.PreviousDeployment);
    }

    [Fact]
    public void GetRecent_NormalizesWhitespaceAndCase()
    {
        var record = CreateStore().GetRecent("  CHECKOUT-API  ");

        Assert.NotNull(record);
        Assert.Equal("checkout-api", record.Service);
    }

    [Fact]
    public void GetRecent_ReturnsNullForUnknownService()
    {
        Assert.Null(CreateStore().GetRecent("missing-api"));
    }
}
