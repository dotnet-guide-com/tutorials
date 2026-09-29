using ProviderAgnosticChatGateway.Providers;

namespace ProviderAgnosticChatGateway.Tests;

public sealed class ProviderRegistryTests
{
    [Fact]
    public void Resolve_UsesDefaultProvider_WhenNoProviderIsRequested()
    {
        using ProviderRegistry registry = CreateRegistry();
        ProviderDescriptor provider = registry.Resolve();
        Assert.Equal("fake", provider.Name);
    }

    [Fact]
    public void Resolve_IsCaseInsensitive()
    {
        using ProviderRegistry registry = CreateRegistry();
        ProviderDescriptor provider = registry.Resolve("FAKE");
        Assert.Equal("fake", provider.Name);
    }

    [Fact]
    public void Resolve_ThrowsForUnknownProvider()
    {
        using ProviderRegistry registry = CreateRegistry();
        Assert.Throws<KeyNotFoundException>(
            () => registry.Resolve("missing-provider"));
    }

    [Fact]
    public void Add_RejectsDuplicateProviderNames()
    {
        using ProviderRegistry registry = CreateRegistry();
        FakeChatClient duplicateClient = new();

        ProviderDescriptor duplicate =
            new(
                "FAKE",
                duplicateClient,
                Capabilities());

        Assert.Throws<InvalidOperationException>(
            () => registry.Add(duplicate));

        duplicateClient.Dispose();
    }

    [Fact]
    public void Resolve_CanDisableProviderSelection()
    {
        using ProviderRegistry registry =
            new(
                defaultProvider: "fake",
                allowProviderSelection: false);

        registry.Add(
            new ProviderDescriptor(
                "fake",
                new FakeChatClient(),
                Capabilities()));

        registry.Add(
            new ProviderDescriptor(
                "other",
                new FakeChatClient(),
                Capabilities()));

        Assert.Throws<InvalidOperationException>(
            () => registry.Resolve("other"));

        Assert.Equal("fake", registry.Resolve().Name);
    }

    [Fact]
    public void Providers_ReturnsRegisteredDescriptors()
    {
        using ProviderRegistry registry = CreateRegistry();
        Assert.Single(registry.Providers);
        Assert.Single(registry.ProviderNames);
    }

    private static ProviderRegistry CreateRegistry()
    {
        ProviderRegistry registry = new("fake");
        registry.Add(
            new ProviderDescriptor(
                "fake",
                new FakeChatClient(),
                Capabilities()));
        return registry;
    }

    private static ProviderCapabilities Capabilities() =>
        new(
            Streaming: true,
            Tools: true,
            ProviderManagedConversation: false);
}
