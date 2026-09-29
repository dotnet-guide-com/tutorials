using Microsoft.Extensions.Logging.Abstractions;
using ProviderAgnosticChatGateway.Conversations;
using ProviderAgnosticChatGateway.Gateway;
using ProviderAgnosticChatGateway.Providers;

namespace ProviderAgnosticChatGateway.Tests;

public sealed class GatewayTests
{
    [Fact]
    public async Task SendAsync_ReturnsProviderNeutralResponse()
    {
        FakeChatClient fake = new() { ResponseText = "HELLO" };
        using ProviderRegistry registry = CreateRegistry(fake);
        using ConversationStore store = new(10);
        ChatGateway gateway = CreateGateway(registry, store);

        ChatResult result =
            await gateway.SendAsync(
                new ChatRequest("hello"));

        Assert.Equal("fake", result.Provider);
        Assert.Equal("HELLO", result.Text);
        Assert.False(string.IsNullOrWhiteSpace(result.ConversationId));
        Assert.Single(fake.Requests);
    }

    [Fact]
    public async Task SendAsync_CarriesHistoryIntoNextTurn()
    {
        FakeChatClient fake = new();
        using ProviderRegistry registry = CreateRegistry(fake);
        using ConversationStore store = new(10);
        ChatGateway gateway = CreateGateway(registry, store);

        ChatResult first =
            await gateway.SendAsync(
                new ChatRequest("first"));

        await gateway.SendAsync(
            new ChatRequest(
                "second",
                ConversationId: first.ConversationId));

        Assert.Equal(2, fake.Requests.Count);
        Assert.Equal(3, fake.Requests[1].Count);
    }

    [Fact]
    public async Task SendAsync_AddsToolOnlyWhenRequested()
    {
        FakeChatClient fake = new();
        using ProviderRegistry registry = CreateRegistry(fake);
        using ConversationStore store = new(10);
        ChatGateway gateway = CreateGateway(registry, store);

        await gateway.SendAsync(
            new ChatRequest(
                "convert 0 C to F",
                UseTools: true));

        Assert.NotNull(fake.LastOptions);
        Assert.NotNull(fake.LastOptions!.Tools);
        Assert.Single(fake.LastOptions.Tools);
    }

    [Fact]
    public async Task SendAsync_KeepsConversationHistoryBounded()
    {
        FakeChatClient fake = new();
        using ProviderRegistry registry = CreateRegistry(fake);
        using ConversationStore store = new(2);
        ChatGateway gateway = CreateGateway(registry, store);

        ChatResult first =
            await gateway.SendAsync(
                new ChatRequest("first"));

        await gateway.SendAsync(
            new ChatRequest(
                "second",
                ConversationId: first.ConversationId));

        Assert.True(
            store.TryGet(
                first.ConversationId,
                out ConversationState? state));

        Assert.NotNull(state);
        Assert.Equal(2, state!.MessageCount);
    }

    [Fact]
    public async Task StreamAsync_EmitsMetadataDeltasAndDone_AndPersistsHistory()
    {
        FakeChatClient fake = new()
        {
            StreamingParts = ["A", "B"]
        };

        using ProviderRegistry registry = CreateRegistry(fake);
        using ConversationStore store = new(10);
        ChatGateway gateway = CreateGateway(registry, store);

        List<ChatStreamEvent> events = [];
        await foreach (
            ChatStreamEvent item
            in gateway.StreamAsync(
                new ChatRequest("stream this")))
        {
            events.Add(item);
        }

        Assert.Equal("meta", events.First().Type);
        Assert.Equal("done", events.Last().Type);
        Assert.Equal(
            "AB",
            string.Concat(
                events
                    .Where(e => e.Type == "delta")
                    .Select(e => e.Text)));

        string conversationId = events.First().ConversationId!;
        Assert.True(store.TryGet(conversationId, out ConversationState? state));
        Assert.Equal(2, state!.MessageCount);
    }

    [Fact]
    public async Task SendAsync_RejectsProviderChangeForExistingConversation()
    {
        FakeChatClient fake = new();
        using ProviderRegistry registry = CreateRegistry(fake);
        registry.Add(
            new ProviderDescriptor(
                "other",
                new FakeChatClient(),
                Capabilities()));

        using ConversationStore store = new(10);
        ChatGateway gateway = CreateGateway(registry, store);

        ChatResult first =
            await gateway.SendAsync(
                new ChatRequest("first"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => gateway.SendAsync(
                new ChatRequest(
                    "second",
                    Provider: "other",
                    ConversationId: first.ConversationId)));
    }

    private static ProviderRegistry CreateRegistry(
        FakeChatClient client)
    {
        ProviderRegistry registry = new("fake");
        registry.Add(
            new ProviderDescriptor(
                "fake",
                client,
                Capabilities()));
        return registry;
    }

    private static ProviderCapabilities Capabilities() =>
        new(
            Streaming: true,
            Tools: true,
            ProviderManagedConversation: false);

    private static ChatGateway CreateGateway(
        ProviderRegistry registry,
        ConversationStore store) =>
        new(
            registry,
            store,
            maxInputCharacters: 8000,
            NullLogger<ChatGateway>.Instance);
}
