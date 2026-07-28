using Microsoft.EntityFrameworkCore;
using TransactionalOutboxMinimal.Data;
using TransactionalOutboxMinimal.Domain;
using TransactionalOutboxMinimal.Messaging;
using TransactionalOutboxMinimal.Services;

namespace TransactionalOutboxMinimal.Tests;

public sealed class OutboxFlowTests
{
    private static OrdersDbContext CreateContext(string dbPath)
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        var context = new OrdersDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _fixedTime;

        public FakeTimeProvider(DateTimeOffset fixedTime)
        {
            _fixedTime = fixedTime;
        }

        public override DateTimeOffset GetUtcNow() => _fixedTime;
    }

    [Fact]
    public async Task Creating_order_writes_order_and_outbox_together()
    {
        string dbPath = Path.Combine(
            Path.GetTempPath(),
            $"test-{Guid.NewGuid():N}.db");
        try
        {
            var dbContext = CreateContext(dbPath);
            var timeProvider = new FakeTimeProvider(
                new DateTimeOffset(2026, 7, 28, 12, 0, 0, TimeSpan.Zero));
            var orderService = new OrderService(dbContext, timeProvider);

            Guid orderId = await orderService.CreateAsync(
                customerId: Guid.NewGuid(),
                total: 49.99m);

            int orderCount = await dbContext.Orders.CountAsync();
            int outboxCount = await dbContext.OutboxMessages.CountAsync();
            int pendingCount = await dbContext.OutboxMessages
                .CountAsync(m => m.ProcessedOnUtc == null);

            Assert.Equal(1, orderCount);
            Assert.Equal(1, outboxCount);
            Assert.Equal(1, pendingCount);
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task Relay_publishes_pending_message_and_marks_it_processed()
    {
        string dbPath = Path.Combine(
            Path.GetTempPath(),
            $"test-{Guid.NewGuid():N}.db");
        try
        {
            var dbContext = CreateContext(dbPath);
            var timeProvider = new FakeTimeProvider(
                new DateTimeOffset(2026, 7, 28, 12, 0, 0, TimeSpan.Zero));
            var orderService = new OrderService(dbContext, timeProvider);
            var publisher = new InMemoryPublisher();
            var relay = new OutboxRelay(dbContext, publisher, timeProvider);

            await orderService.CreateAsync(
                customerId: Guid.NewGuid(),
                total: 49.99m);

            int published = await relay.DispatchPendingAsync();

            Assert.Equal(1, published);
            Assert.Equal(1, publisher.PublishedMessageIds.Count);

            int processedCount = await dbContext.OutboxMessages
                .CountAsync(m => m.ProcessedOnUtc != null);

            Assert.Equal(1, processedCount);

            int pendingCount = await dbContext.OutboxMessages
                .CountAsync(m => m.ProcessedOnUtc == null);

            Assert.Equal(0, pendingCount);
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task Second_relay_run_does_not_republish_processed_message()
    {
        string dbPath = Path.Combine(
            Path.GetTempPath(),
            $"test-{Guid.NewGuid():N}.db");
        try
        {
            var dbContext = CreateContext(dbPath);
            var timeProvider = new FakeTimeProvider(
                new DateTimeOffset(2026, 7, 28, 12, 0, 0, TimeSpan.Zero));
            var orderService = new OrderService(dbContext, timeProvider);
            var publisher = new InMemoryPublisher();
            var relay = new OutboxRelay(dbContext, publisher, timeProvider);

            await orderService.CreateAsync(
                customerId: Guid.NewGuid(),
                total: 49.99m);

            await relay.DispatchPendingAsync();

            int secondRun = await relay.DispatchPendingAsync();

            Assert.Equal(0, secondRun);
            Assert.Equal(1, publisher.PublishedMessageIds.Count);
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }
}