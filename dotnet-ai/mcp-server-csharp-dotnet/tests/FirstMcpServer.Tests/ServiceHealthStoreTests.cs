using FirstMcpServer.Models;
using FirstMcpServer.Stores;

namespace FirstMcpServer.Tests;

public sealed class ServiceHealthStoreTests
{
    private static ServiceHealthStore CreateStore()
        => ServiceHealthStore.FromRecords(
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

    [Fact]
    public void Get_ReturnsKnownService()
    {
        var record = CreateStore().Get("checkout-api");

        Assert.NotNull(record);
        Assert.Equal("E-101", record.EvidenceId);
        Assert.Equal("degraded", record.Status);
    }

    [Fact]
    public void Get_NormalizesWhitespaceAndCase()
    {
        var record = CreateStore().Get("  CHECKOUT-API  ");

        Assert.NotNull(record);
        Assert.Equal("checkout-api", record.Service);
    }

    [Fact]
    public void Get_ReturnsNullForUnknownService()
    {
        Assert.Null(CreateStore().Get("missing-api"));
    }
}
