using Microsoft.Extensions.AI;
using ProviderAgnosticChatGateway.Providers;

namespace ProviderAgnosticChatGateway.Tests;

public sealed class ProviderClientFactoryTests
{
    [Fact]
    public void CreateOllama_ConstructsIChatClientWithoutNetworkCall()
    {
        using IChatClient client =
            ProviderClientFactory.CreateOllama(
                "http://localhost:11434",
                "verification-placeholder");

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateOpenAI_ConstructsIChatClientWithoutNetworkCall()
    {
        using IChatClient client =
            ProviderClientFactory.CreateOpenAI(
                "test-api-key",
                "test-model");

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateOpenAICompatible_ConstructsIChatClientWithoutNetworkCall()
    {
        using IChatClient client =
            ProviderClientFactory.CreateOpenAICompatible(
                "https://example.invalid/v1",
                "test-api-key",
                "test-model");

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateAzureOpenAIWithApiKey_ConstructsIChatClientWithoutNetworkCall()
    {
        using IChatClient client =
            ProviderClientFactory.CreateAzureOpenAIWithApiKey(
                "https://example.openai.azure.com/",
                "test-api-key",
                "test-deployment");

        Assert.NotNull(client);
    }
}
