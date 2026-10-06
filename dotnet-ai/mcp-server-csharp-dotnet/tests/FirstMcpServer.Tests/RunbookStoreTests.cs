using FirstMcpServer.Models;
using FirstMcpServer.Stores;

namespace FirstMcpServer.Tests;

public sealed class RunbookStoreTests
{
    private static RunbookStore CreateStore()
        => RunbookStore.FromRecords(
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

    [Fact]
    public void Get_ReturnsKnownRunbook()
    {
        var record = CreateStore().Get("checkout-api");

        Assert.NotNull(record);
        Assert.Equal("RB-CHECKOUT-03", record.RunbookId);
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
