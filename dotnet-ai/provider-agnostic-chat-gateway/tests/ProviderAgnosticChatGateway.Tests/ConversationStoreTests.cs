using ProviderAgnosticChatGateway.Conversations;

namespace ProviderAgnosticChatGateway.Tests;

public sealed class ConversationStoreTests
{
    [Fact]
    public void GetOrCreate_CreatesConversation_WhenIdIsMissing()
    {
        using ConversationStore store = new(10);
        ConversationState state = store.GetOrCreate(null, "fake");

        Assert.False(string.IsNullOrWhiteSpace(state.Id));
        Assert.Equal("fake", state.ProviderName);
    }

    [Fact]
    public void GetOrCreate_ReturnsExistingConversation()
    {
        using ConversationStore store = new(10);
        ConversationState first = store.GetOrCreate(null, "fake");
        ConversationState second = store.GetOrCreate(first.Id, "fake");

        Assert.Same(first, second);
    }

    [Fact]
    public void GetOrCreate_RejectsProviderChange()
    {
        using ConversationStore store = new(10);
        ConversationState state = store.GetOrCreate(null, "fake");

        Assert.Throws<InvalidOperationException>(
            () => store.GetOrCreate(state.Id, "other"));
    }

    [Fact]
    public void GetOrCreate_RejectsUnknownConversationId()
    {
        using ConversationStore store = new(10);

        Assert.Throws<KeyNotFoundException>(
            () => store.GetOrCreate("not-created-here", "fake"));
    }
}
