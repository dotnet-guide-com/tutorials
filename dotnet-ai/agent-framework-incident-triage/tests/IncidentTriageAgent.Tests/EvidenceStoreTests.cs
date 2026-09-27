using IncidentTriageAgent.Evidence;
using Xunit;

namespace IncidentTriageAgent.Tests;

public sealed class EvidenceStoreTests
{
    [Fact]
    public void GetLatest_ReturnsCheckoutHealthEvidence()
    {
        EvidenceStore store = new();

        EvidenceItem? evidence =
            store.GetLatest(
                "checkout-api",
                "service_health");

        Assert.NotNull(evidence);

        Assert.Equal("E-101", evidence.Id);
        Assert.Equal("degraded", evidence.GetFact("status"));
        Assert.Equal("18.7%", evidence.GetFact("error_rate"));
        Assert.Equal(
            "96%",
            evidence.GetFact("database_pool_utilization"));
    }


    [Fact]
    public void GetLatest_NormalizesServiceName()
    {
        EvidenceStore store = new();

        EvidenceItem? evidence =
            store.GetLatest(
                "  CHECKOUT-API  ",
                "deployment");

        Assert.NotNull(evidence);
        Assert.Equal("E-201", evidence.Id);
        Assert.Equal(
            "deploy-1842",
            evidence.GetFact("deployment"));
    }


    [Fact]
    public void DeploymentEvidence_DoesNotInventMissingTimestamp()
    {
        EvidenceStore store = new();

        EvidenceItem? evidence =
            store.GetLatest(
                "checkout-api",
                "deployment");

        Assert.NotNull(evidence);

        Assert.Equal(
            "deploy-1839",
            evidence.GetFact("previous_deployment"));

        Assert.Null(
            evidence.GetFact(
                "previous_deployment_completed_at"));
    }


    [Fact]
    public void UnknownService_ReturnsNoEvidence()
    {
        EvidenceStore store = new();

        EvidenceItem? evidence =
            store.GetLatest(
                "unknown-api",
                "service_health");

        Assert.Null(evidence);
    }


    [Fact]
    public void GetForService_ReturnsBothCheckoutEvidenceItems()
    {
        EvidenceStore store = new();

        IReadOnlyList<EvidenceItem> evidence =
            store.GetForService("checkout-api");

        Assert.Equal(2, evidence.Count);

        Assert.Contains(
            evidence,
            item => item.Id == "E-101");

        Assert.Contains(
            evidence,
            item => item.Id == "E-201");
    }
}