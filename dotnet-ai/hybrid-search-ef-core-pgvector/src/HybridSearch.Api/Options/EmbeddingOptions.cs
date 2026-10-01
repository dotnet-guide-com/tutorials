namespace HybridSearch.Api.Options;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    public string Provider { get; init; } = "Ollama";
    public string Endpoint { get; init; } = "http://localhost:11434";
    public string Model { get; init; } = "nomic-embed-text";
}

public static class EmbeddingDimensions
{
    // Live-verified with Ollama nomic-embed-text on 2026-09-30.
    public const int Value = 768;
}