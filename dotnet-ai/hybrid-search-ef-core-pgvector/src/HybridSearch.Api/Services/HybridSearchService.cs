using HybridSearch.Api.Data;
using HybridSearch.Api.Models;
using HybridSearch.Api.Options;
using HybridSearch.Api.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace HybridSearch.Api.Services;

public sealed class HybridSearchService(
    SearchDbContext db,
    IEmbeddingGenerator<string, Embedding<float>> embeddings)
{
    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            throw new ArgumentException("Query is required.", nameof(request));
        if (request.Top is < 1 or > 20)
            throw new ArgumentOutOfRangeException(nameof(request), "Top must be between 1 and 20.");

        SearchMode mode = SearchModeParser.Parse(request.Mode);
        int candidateLimit = Math.Min(Math.Max(request.Top * 4, 20), 100);

        if (mode == SearchMode.Keyword)
        {
            IReadOnlyList<RankedCandidate> keyword = await KeywordAsync(
                request.Query.Trim(), candidateLimit, cancellationToken);
            return keyword.Take(request.Top)
                .Select(x => new SearchHit(
                    x.Id, x.DocumentId, x.ChunkNumber, x.Title, x.Text,
                    x.SourceScore, x.Rank, null))
                .ToList();
        }

        if (mode == SearchMode.Vector)
        {
            IReadOnlyList<RankedCandidate> vector = await VectorAsync(
                request.Query.Trim(), candidateLimit, cancellationToken);
            return vector.Take(request.Top)
                .Select(x => new SearchHit(
                    x.Id, x.DocumentId, x.ChunkNumber, x.Title, x.Text,
                    x.SourceScore, null, x.Rank))
                .ToList();
        }

        IReadOnlyList<RankedCandidate> keywordCandidates = await KeywordAsync(
            request.Query.Trim(), candidateLimit, cancellationToken);
        IReadOnlyList<RankedCandidate> vectorCandidates = await VectorAsync(
            request.Query.Trim(), candidateLimit, cancellationToken);

        return ReciprocalRankFusion.Fuse(
            keywordCandidates,
            vectorCandidates,
            request.Top);
    }

    private async Task<IReadOnlyList<RankedCandidate>> KeywordAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var rows = await db.DocumentChunks
            .AsNoTracking()
            .Where(x => x.SearchVector.Matches(
                EF.Functions.WebSearchToTsQuery("english", query)))
            .OrderByDescending(x => x.SearchVector.Rank(
                EF.Functions.WebSearchToTsQuery("english", query)))
            .Take(limit)
            .Select(x => new
            {
                x.Id,
                x.DocumentId,
                x.ChunkNumber,
                x.Title,
                x.Text,
                Score = x.SearchVector.Rank(
                    EF.Functions.WebSearchToTsQuery("english", query))
            })
            .ToListAsync(cancellationToken);

        return rows.Select((x, index) => new RankedCandidate(
                x.Id,
                x.DocumentId,
                x.ChunkNumber,
                x.Title,
                x.Text,
                index + 1,
                x.Score))
            .ToList();
    }

    private async Task<IReadOnlyList<RankedCandidate>> VectorAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        ReadOnlyMemory<float> embedding = await embeddings.GenerateVectorAsync(query);
        if (embedding.Length != EmbeddingDimensions.Value)
        {
            throw new InvalidOperationException(
                $"Embedding dimension mismatch. Expected {EmbeddingDimensions.Value}; " +
                $"received {embedding.Length}.");
        }

        Vector queryVector = new(embedding.ToArray());

        var rows = await db.DocumentChunks
            .AsNoTracking()
            .Where(x => x.Embedding != null)
            .OrderBy(x => x.Embedding!.CosineDistance(queryVector))
            .Take(limit)
            .Select(x => new
            {
                x.Id,
                x.DocumentId,
                x.ChunkNumber,
                x.Title,
                x.Text,
                Distance = x.Embedding!.CosineDistance(queryVector)
            })
            .ToListAsync(cancellationToken);

        return rows.Select((x, index) => new RankedCandidate(
                x.Id,
                x.DocumentId,
                x.ChunkNumber,
                x.Title,
                x.Text,
                index + 1,
                1.0 - x.Distance))
            .ToList();
    }
}