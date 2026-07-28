using Microsoft.EntityFrameworkCore;
using TransactionalOutboxMinimal.Domain;
using TransactionalOutboxMinimal.Messaging;

namespace TransactionalOutboxMinimal.Data;

public sealed class OrdersDbContext(
    DbContextOptions<OrdersDbContext> options)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OutboxMessage> OutboxMessages =>
        Set<OutboxMessage>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(message => message.Id);

            entity.Property(message => message.Type)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(message => message.Payload)
                .IsRequired();

            entity.HasIndex(message => new
            {
                message.ProcessedOnUtc,
                message.OccurredOnUtc
            });
        });
    }
}