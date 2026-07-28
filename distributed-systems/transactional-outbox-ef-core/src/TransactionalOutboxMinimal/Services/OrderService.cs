using System.Text.Json;
using TransactionalOutboxMinimal.Data;
using TransactionalOutboxMinimal.Domain;
using TransactionalOutboxMinimal.Messaging;

namespace TransactionalOutboxMinimal.Services;

public sealed class OrderService(
    OrdersDbContext dbContext,
    TimeProvider timeProvider)
{
    public async Task<Guid> CreateAsync(
        Guid customerId,
        decimal total,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        Guid orderId = Guid.NewGuid();

        var order = new Order(
            orderId,
            customerId,
            total,
            now);

        var integrationEvent = new OrderPlaced(
            order.Id,
            order.CustomerId,
            order.Total,
            now);

        var outboxMessage = new OutboxMessage(
            Guid.NewGuid(),
            typeof(OrderPlaced).FullName
                ?? nameof(OrderPlaced),
            JsonSerializer.Serialize(integrationEvent),
            now);

        dbContext.Orders.Add(order);
        dbContext.OutboxMessages.Add(outboxMessage);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return orderId;
    }
}