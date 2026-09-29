using System.Collections.Concurrent;

namespace ProviderAgnosticChatGateway.Conversations;

/// <summary>
/// A deliberately small in-memory conversation store for the tutorial sample.
/// A conversation is permanently bound to the provider that created it.
/// </summary>
public sealed class ConversationStore : IDisposable
{
    private readonly ConcurrentDictionary<string, ConversationState> _conversations =
        new(StringComparer.OrdinalIgnoreCase);

    public ConversationStore(int maxMessages)
    {
        if (maxMessages < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxMessages),
                "At least two messages must be retained.");
        }

        MaxMessages = maxMessages;
    }

    public int MaxMessages { get; }

    public ConversationState GetOrCreate(
        string? conversationId,
        string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        if (string.IsNullOrWhiteSpace(conversationId))
        {
            string id = Guid.NewGuid().ToString("N");
            ConversationState created =
                new(id, providerName.Trim());

            if (!_conversations.TryAdd(id, created))
            {
                created.Dispose();
                throw new InvalidOperationException(
                    "Unable to create a conversation.");
            }

            return created;
        }

        string normalizedId = conversationId.Trim();
        if (!_conversations.TryGetValue(
                normalizedId,
                out ConversationState? existing))
        {
            throw new KeyNotFoundException(
                $"Conversation '{normalizedId}' was not found.");
        }

        if (!existing.ProviderName.Equals(
                providerName.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Conversation '{normalizedId}' is bound to provider " +
                $"'{existing.ProviderName}' and cannot be continued with " +
                $"'{providerName}'.");
        }

        return existing;
    }

    public bool TryGet(
        string conversationId,
        out ConversationState? state) =>
        _conversations.TryGetValue(conversationId, out state);

    public void Dispose()
    {
        foreach (ConversationState state in _conversations.Values)
        {
            state.Dispose();
        }

        _conversations.Clear();
    }
}
