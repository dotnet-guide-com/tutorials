namespace TransactionalOutboxMinimal.Messaging;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public OutboxMessage(
        Guid id,
        string type,
        string payload,
        DateTimeOffset occurredOnUtc)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset OccurredOnUtc { get; private set; }

    public DateTimeOffset? ProcessedOnUtc { get; private set; }

    public void MarkProcessed(DateTimeOffset processedOnUtc)
    {
        ProcessedOnUtc = processedOnUtc;
    }
}