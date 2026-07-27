using ArchitectureGuard.Application;
using ArchitectureGuard.Domain;

namespace ArchitectureGuard.Infrastructure;

public sealed class InMemoryOrderRepository
{
    public GetOrderSummary GetSummary(Guid orderId)
    {
        var order = new Order(orderId, 123.45m);
        return new GetOrderSummary(order.Id);
    }
}