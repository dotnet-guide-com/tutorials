using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

namespace ProviderAgnosticChatGateway.Providers;

/// <summary>
/// Keeps provider-specific SDK construction at the edge of the application.
/// Every returned client is exposed to the gateway as IChatClient and is
/// wrapped with the Microsoft.Extensions.AI function-invocation pipeline.
/// </summary>
public static class ProviderClientFactory
{
    public static IChatClient CreateOpenAI(
        string apiKey,
        string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        IChatClient client =
            new OpenAI.Chat.ChatClient(model, apiKey)
                .AsIChatClient();

        return AddCommonPipeline(client);
    }

    public static IChatClient CreateOpenAICompatible(
        string endpoint,
        string apiKey,
        string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        OpenAIClientOptions options =
            new()
            {
                Endpoint = new Uri(endpoint)
            };

        OpenAIClient client =
            new(
                new System.ClientModel.ApiKeyCredential(apiKey),
                options);

        IChatClient chatClient =
            client
                .GetChatClient(model)
                .AsIChatClient();

        return AddCommonPipeline(chatClient);
    }

    public static IChatClient CreateAzureOpenAIWithApiKey(
        string endpoint,
        string apiKey,
        string deployment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(deployment);

        AzureOpenAIClient client =
            new(
                new Uri(endpoint),
                new AzureKeyCredential(apiKey));

        IChatClient chatClient =
            client
                .GetChatClient(deployment)
                .AsIChatClient();

        return AddCommonPipeline(chatClient);
    }

    public static IChatClient CreateAzureOpenAIWithDefaultCredential(
        string endpoint,
        string deployment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(deployment);

        AzureOpenAIClient client =
            new(
                new Uri(endpoint),
                new DefaultAzureCredential());

        IChatClient chatClient =
            client
                .GetChatClient(deployment)
                .AsIChatClient();

        return AddCommonPipeline(chatClient);
    }

    public static IChatClient CreateOllama(
        string endpoint,
        string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        IChatClient client =
            new OllamaApiClient(
                new Uri(endpoint),
                model);

        return AddCommonPipeline(client);
    }

    private static IChatClient AddCommonPipeline(
        IChatClient client) =>
        new ChatClientBuilder(client)
            .UseFunctionInvocation()
            .Build();
}
