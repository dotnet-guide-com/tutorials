namespace ProviderAgnosticChatGateway.Providers;

/// <summary>
/// Describes capabilities that this gateway declares for a configured provider.
/// The gateway uses this metadata for request validation; it does not infer
/// capabilities from model output at runtime.
/// </summary>
public sealed record ProviderCapabilities(
    bool Streaming,
    bool Tools,
    bool ProviderManagedConversation);
