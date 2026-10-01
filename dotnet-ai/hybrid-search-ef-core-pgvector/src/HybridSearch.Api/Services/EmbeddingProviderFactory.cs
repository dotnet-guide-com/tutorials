using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI.Embeddings;

namespace HybridSearch.Api.Services;

public static class EmbeddingProviderFactory
{
    public static IEmbeddingGenerator<string, Embedding<float>> CreateOllama(
        string endpoint,
        string model) =>
        new OllamaApiClient(new Uri(endpoint), model);

    // Compile/construction verified optional provider example. No live cloud
    // request is made by the repository CI or the default verification script.
    public static IEmbeddingGenerator<string, Embedding<float>> CreateOpenAI(
        string apiKey,
        string model) =>
        new EmbeddingClient(model, apiKey).AsIEmbeddingGenerator();

    // Compile/construction verified optional provider example. No live cloud
    // request is made by the repository CI or the default verification script.
    public static IEmbeddingGenerator<string, Embedding<float>> CreateAzureOpenAI(
        string endpoint,
        string apiKey,
        string deployment)
    {
        AzureOpenAIClient client = new(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey));

        return client.GetEmbeddingClient(deployment).AsIEmbeddingGenerator();
    }
}