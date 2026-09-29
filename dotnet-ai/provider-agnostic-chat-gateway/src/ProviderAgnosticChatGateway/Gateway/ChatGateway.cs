using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using ProviderAgnosticChatGateway.Conversations;
using ProviderAgnosticChatGateway.Providers;
using ProviderAgnosticChatGateway.Tools;

namespace ProviderAgnosticChatGateway.Gateway;

/// <summary>
/// Provider-neutral chat orchestration. The class depends on IChatClient through
/// ProviderDescriptor and contains no provider SDK-specific request logic.
/// </summary>
public sealed class ChatGateway
{
    private readonly ProviderRegistry _providers;
    private readonly ConversationStore _conversations;
    private readonly int _maxInputCharacters;
    private readonly ILogger<ChatGateway> _logger;
    private readonly AIFunction _temperatureTool;

    public ChatGateway(
        ProviderRegistry providers,
        ConversationStore conversations,
        int maxInputCharacters,
        ILogger<ChatGateway> logger)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(conversations);
        ArgumentNullException.ThrowIfNull(logger);

        if (maxInputCharacters < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxInputCharacters));
        }

        _providers = providers;
        _conversations = conversations;
        _maxInputCharacters = maxInputCharacters;
        _logger = logger;

        _temperatureTool = AIFunctionFactory.Create(
            (Func<double, string, string, double>)ConvertTemperatureWithLogging,
            name: "convert_temperature",
            description:
                "Convert a temperature value between Celsius, Fahrenheit and Kelvin.");
    }

    public async Task<ChatResult> SendAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        ProviderDescriptor provider =
            ResolveProviderForRequest(request);

        ConversationState conversation =
            _conversations.GetOrCreate(
                request.ConversationId,
                provider.Name);

        await conversation.Gate.WaitAsync(cancellationToken);
        try
        {
            ChatMessage userMessage =
                new(ChatRole.User, request.Message.Trim());

            List<ChatMessage> messages =
                [.. conversation.SnapshotMessages(), userMessage];

            ChatOptions? options =
                CreateOptions(request, provider);

            ChatResponse response =
                await provider.Client.GetResponseAsync(
                    messages,
                    options,
                    cancellationToken);

            conversation.AppendMessages(
                [userMessage, .. response.Messages],
                _conversations.MaxMessages);

            _logger.LogInformation(
                "Chat completed with provider {Provider} for conversation {ConversationId}",
                provider.Name,
                conversation.Id);

            return new ChatResult(
                conversation.Id,
                provider.Name,
                response.Text);
        }
        finally
        {
            conversation.Gate.Release();
        }
    }

    public async IAsyncEnumerable<ChatStreamEvent> StreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        ProviderDescriptor provider =
            ResolveProviderForRequest(request);

        if (!provider.Capabilities.Streaming)
        {
            throw new InvalidOperationException(
                $"Provider '{provider.Name}' is not configured for streaming.");
        }

        ConversationState conversation =
            _conversations.GetOrCreate(
                request.ConversationId,
                provider.Name);

        await conversation.Gate.WaitAsync(cancellationToken);
        try
        {
            yield return new ChatStreamEvent(
                Type: "meta",
                ConversationId: conversation.Id,
                Provider: provider.Name);

            ChatMessage userMessage =
                new(ChatRole.User, request.Message.Trim());

            List<ChatMessage> messages =
                [.. conversation.SnapshotMessages(), userMessage];

            ChatOptions? options =
                CreateOptions(request, provider);

            List<ChatResponseUpdate> updates = [];

            await foreach (
                ChatResponseUpdate update
                in provider.Client.GetStreamingResponseAsync(
                    messages,
                    options,
                    cancellationToken)
                .WithCancellation(cancellationToken))
            {
                updates.Add(update);

                if (!string.IsNullOrEmpty(update.Text))
                {
                    yield return new ChatStreamEvent(
                        Type: "delta",
                        ConversationId: conversation.Id,
                        Provider: provider.Name,
                        Text: update.Text);
                }
            }

            ChatResponse response = updates.ToChatResponse();

            conversation.AppendMessages(
                [userMessage, .. response.Messages],
                _conversations.MaxMessages);

            _logger.LogInformation(
                "Streaming chat completed with provider {Provider} for conversation {ConversationId}",
                provider.Name,
                conversation.Id);

            yield return new ChatStreamEvent(
                Type: "done",
                ConversationId: conversation.Id,
                Provider: provider.Name);
        }
        finally
        {
            conversation.Gate.Release();
        }
    }

    private ProviderDescriptor ResolveProviderForRequest(
        ChatRequest request)
    {
        ProviderDescriptor provider =
            _providers.Resolve(request.Provider);

        if (request.UseTools && !provider.Capabilities.Tools)
        {
            throw new InvalidOperationException(
                $"Provider '{provider.Name}' is not configured for tools.");
        }

        return provider;
    }

    private ChatOptions? CreateOptions(
        ChatRequest request,
        ProviderDescriptor provider)
    {
        if (!request.UseTools)
        {
            return null;
        }

        if (!provider.Capabilities.Tools)
        {
            throw new InvalidOperationException(
                $"Provider '{provider.Name}' is not configured for tools.");
        }

        return new ChatOptions
        {
            Tools = [_temperatureTool]
        };
    }

    private double ConvertTemperatureWithLogging(
        double value,
        string fromUnit,
        string toUnit)
    {
        double result = TemperatureTool.ConvertTemperature(
            value,
            fromUnit,
            toUnit);

        _logger.LogInformation(
            "TOOL CALLED: {ToolName} value={Value} from={FromUnit} to={ToUnit} result={Result}",
            "convert_temperature",
            value,
            fromUnit,
            toUnit,
            result);

        return result;
    }
    private void ValidateRequest(ChatRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException(
                "Message is required.",
                nameof(request));
        }

        if (request.Message.Length > _maxInputCharacters)
        {
            throw new ArgumentException(
                $"Message exceeds the {_maxInputCharacters}-character limit.",
                nameof(request));
        }
    }
}
