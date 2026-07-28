namespace TransactionalOutboxMinimal.Messaging;

public sealed class InMemoryPublisher : IPublisher
{
    private readonly List<Guid> _publishedMessageIds = [];

    public IReadOnlyCollection<Guid> PublishedMessageIds =>
        _publishedMessageIds;

    public Task PublishAsync(
        Guid messageId,
        string type,
        string payload,
        CancellationToken cancellationToken = default)
    {
        _publishedMessageIds.Add(messageId);
        return Task.CompletedTask;
    }
}