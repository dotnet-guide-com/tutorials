namespace ProviderAgnosticChatGateway.Gateway;

public sealed record ChatRequest(
    string Message,
    string? Provider = null,
    string? ConversationId = null,
    bool UseTools = false);
