using Microsoft.EntityFrameworkCore;
using TransactionalOutboxMinimal.Data;

namespace TransactionalOutboxMinimal.Messaging;

public sealed class OutboxRelay(
    OrdersDbContext dbContext,
    IPublisher publisher,
    TimeProvider timeProvider)
{
    public async Task<int> DispatchPendingAsync(
        CancellationToken cancellationToken = default)
    {
        List<OutboxMessage> messages =
            await dbContext.OutboxMessages
                .Where(message =>
                    message.ProcessedOnUtc == null)
                .ToListAsync(cancellationToken);

        messages = messages
                .OrderBy(message =>
                    message.OccurredOnUtc)
                .ToList();

        foreach (OutboxMessage message in messages)
        {
            await publisher.PublishAsync(
                message.Id,
                message.Type,
                message.Payload,
                cancellationToken);

            message.MarkProcessed(
                timeProvider.GetUtcNow());
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return messages.Count;
    }
}