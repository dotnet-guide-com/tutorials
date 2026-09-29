using Microsoft.Extensions.AI;

namespace ProviderAgnosticChatGateway.Conversations;

/// <summary>
/// In-memory state for one bounded conversation. A semaphore serializes turns
/// so two requests cannot mutate the same history concurrently.
/// </summary>
public sealed class ConversationState : IDisposable
{
    private readonly object _sync = new();
    private readonly List<ChatMessage> _messages = [];

    public ConversationState(
        string id,
        string providerName)
    {
        Id = id;
        ProviderName = providerName;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; }

    public string ProviderName { get; }

    public DateTimeOffset CreatedAt { get; }

    public SemaphoreSlim Gate { get; } = new(1, 1);

    public int MessageCount
    {
        get
        {
            lock (_sync)
            {
                return _messages.Count;
            }
        }
    }

    public IReadOnlyList<ChatMessage> SnapshotMessages()
    {
        lock (_sync)
        {
            return _messages.ToArray();
        }
    }

    internal void AppendMessages(
        IEnumerable<ChatMessage> messages,
        int maxMessages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        if (maxMessages < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMessages));
        }

        lock (_sync)
        {
            _messages.AddRange(messages);

            int removeCount = _messages.Count - maxMessages;
            if (removeCount > 0)
            {
                _messages.RemoveRange(0, removeCount);
            }
        }
    }

    public void Dispose() => Gate.Dispose();
}
