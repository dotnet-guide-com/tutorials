using Microsoft.Extensions.AI;

namespace ProviderAgnosticChatGateway.Providers;

/// <summary>
/// Represents one allow-listed AI provider exposed through the common
/// Microsoft.Extensions.AI IChatClient abstraction.
/// </summary>
public sealed record ProviderDescriptor(
    string Name,
    IChatClient Client,
    ProviderCapabilities Capabilities);
