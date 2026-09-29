namespace ProviderAgnosticChatGateway.Gateway;

public sealed record ChatStreamEvent(
    string Type,
    string? ConversationId = null,
    string? Provider = null,
    string? Text = null);
