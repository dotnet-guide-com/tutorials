using Microsoft.EntityFrameworkCore;
using TransactionalOutboxMinimal.Data;
using TransactionalOutboxMinimal.Domain;
using TransactionalOutboxMinimal.Messaging;
using TransactionalOutboxMinimal.Services;

string dbPath = Path.Combine(
    Path.GetTempPath(),
    $"transactional-outbox-{Guid.NewGuid():N}.db");

try
{
    DbContextOptionsBuilder<OrdersDbContext> optionsBuilder =
        new DbContextOptionsBuilder<OrdersDbContext>()
            .UseSqlite($"Data Source={dbPath}");

    var dbContext = new OrdersDbContext(optionsBuilder.Options);
    await dbContext.Database.EnsureCreatedAsync();

    var timeProvider = TimeProvider.System;
    var orderService = new OrderService(dbContext, timeProvider);
    var publisher = new InMemoryPublisher();
    var relay = new OutboxRelay(dbContext, publisher, timeProvider);

    // 1. Atomic write: order + outbox in one SaveChangesAsync
    Guid orderId = await orderService.CreateAsync(
        customerId: Guid.NewGuid(),
        total: 199.99m);

    int orderCount = await dbContext.Orders.CountAsync();
    int outboxCount = await dbContext.OutboxMessages.CountAsync();

    Console.WriteLine($"Order committed: {orderCount}");
    Console.WriteLine($"Outbox rows committed: {outboxCount}");

    // 2. Confirm pending before relay
    int pendingBefore = await dbContext.OutboxMessages
        .CountAsync(m => m.ProcessedOnUtc == null);

    Console.WriteLine($"Pending before relay: {pendingBefore}");

    // 3. Run relay
    int published = await relay.DispatchPendingAsync();

    Console.WriteLine($"Published by relay: {published}");
    Console.WriteLine($"Published message IDs: {published}");

    // 4. Confirm after relay
    int pendingAfter = await dbContext.OutboxMessages
        .CountAsync(m => m.ProcessedOnUtc == null);

    int processedCount = await dbContext.OutboxMessages
        .CountAsync(m => m.ProcessedOnUtc != null);

    Console.WriteLine($"Pending after relay: {pendingAfter}");
    Console.WriteLine($"Processed outbox rows: {processedCount}");

    // 5. Verify second relay run publishes nothing
    int secondRun = await relay.DispatchPendingAsync();
    if (secondRun == 0)
    {
        Console.WriteLine("Second relay run published 0 messages (expected)");
    }
    else
    {
        Console.WriteLine($"WARNING: Second relay run published {secondRun} messages");
    }
}
finally
{
    if (File.Exists(dbPath))
    {
        File.Delete(dbPath);
        Console.WriteLine($"Deleted temp DB: {dbPath}");
    }
}