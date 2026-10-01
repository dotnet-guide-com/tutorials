namespace HybridSearch.Api.Models;

public sealed record IngestDocumentRequest(
    string DocumentId,
    string Title,
    string Text);

public sealed record IngestDocumentResponse(
    string DocumentId,
    int ChunksStored,
    string EmbeddingModel,
    int EmbeddingDimensions);

public sealed record SearchRequest(
    string Query,
    string Mode = "hybrid",
    int Top = 5);

public sealed record SearchHit(
    Guid Id,
    string DocumentId,
    int ChunkNumber,
    string Title,
    string Text,
    double Score,
    int? KeywordRank,
    int? VectorRank);