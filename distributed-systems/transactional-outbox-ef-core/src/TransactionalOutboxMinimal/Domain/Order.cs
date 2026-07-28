namespace TransactionalOutboxMinimal.Domain;

public sealed class Order
{
    private Order()
    {
    }

    public Order(
        Guid id,
        Guid customerId,
        decimal total,
        DateTimeOffset createdOnUtc)
    {
        Id = id;
        CustomerId = customerId;
        Total = total;
        CreatedOnUtc = createdOnUtc;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public decimal Total { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
}