namespace TransactionalOutboxMinimal.Messaging;

public sealed record OrderPlaced(
    Guid OrderId,
    Guid CustomerId,
    decimal Total,
    DateTimeOffset OccurredOnUtc);