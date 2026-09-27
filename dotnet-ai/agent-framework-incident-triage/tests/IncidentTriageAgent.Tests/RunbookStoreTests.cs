using IncidentTriageAgent.Guidance;
using Xunit;

namespace IncidentTriageAgent.Tests;

public sealed class RunbookStoreTests
{
    [Fact]
    public void GetForService_ReturnsCheckoutRunbook()
    {
        RunbookStore store = new();

        RunbookItem? runbook =
            store.GetForService("checkout-api");

        Assert.NotNull(runbook);

        Assert.Equal(
            "RB-CHECKOUT-03",
            runbook.Id);

        Assert.Equal(
            "Checkout API elevated 5xx response",
            runbook.Title);

        Assert.Equal(
            5,
            runbook.RecommendedChecks.Length);
    }


    [Fact]
    public void GetForService_NormalizesServiceName()
    {
        RunbookStore store = new();

        RunbookItem? runbook =
            store.GetForService(
                "  CHECKOUT-API  ");

        Assert.NotNull(runbook);

        Assert.Equal(
            "RB-CHECKOUT-03",
            runbook.Id);
    }


    [Fact]
    public void UnknownService_ReturnsNoRunbook()
    {
        RunbookStore store = new();

        RunbookItem? runbook =
            store.GetForService(
                "payments-api");

        Assert.Null(runbook);
    }
}