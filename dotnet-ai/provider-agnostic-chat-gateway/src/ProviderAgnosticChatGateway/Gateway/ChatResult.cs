namespace ProviderAgnosticChatGateway.Gateway;

public sealed record ChatResult(
    string ConversationId,
    string Provider,
    string Text);
