using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace ProviderAgnosticChatGateway.Tests;

internal sealed class FakeChatClient : IChatClient
{
    public List<IReadOnlyList<ChatMessage>> Requests { get; } = [];

    public ChatOptions? LastOptions { get; private set; }

    public string ResponseText { get; set; } = "FAKE_OK";

    public IReadOnlyList<string> StreamingParts { get; set; } =
        ["STREAM_", "OK"];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Requests.Add(messages.ToArray());
        LastOptions = options;

        return Task.FromResult(
            new ChatResponse(
                new ChatMessage(
                    ChatRole.Assistant,
                    ResponseText)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Requests.Add(messages.ToArray());
        LastOptions = options;

        foreach (string part in StreamingParts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new ChatResponseUpdate(
                ChatRole.Assistant,
                [new TextContent(part)])
            {
                MessageId = "fake-message-1"
            };

            await Task.Yield();
        }
    }

    public object? GetService(
        Type serviceType,
        object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this)
            ? this
            : null;

    public void Dispose()
    {
    }
}
