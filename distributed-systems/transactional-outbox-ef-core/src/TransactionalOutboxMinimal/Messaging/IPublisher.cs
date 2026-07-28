namespace TransactionalOutboxMinimal.Messaging;

public interface IPublisher
{
    Task PublishAsync(
        Guid messageId,
        string type,
        string payload,
        CancellationToken cancellationToken = default);
}