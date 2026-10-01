using HybridSearch.Api.Data;
using HybridSearch.Api.Models;
using HybridSearch.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Pgvector;

namespace HybridSearch.Api.Services;

public sealed class DocumentIngestionService(
    SearchDbContext db,
    TextChunker chunker,
    IEmbeddingGenerator<string, Embedding<float>> embeddings,
    IOptions<EmbeddingOptions> embeddingOptions)
{
    public async Task<IngestDocumentResponse> IngestAsync(
        IngestDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentId))
            throw new ArgumentException("DocumentId is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Text))
            throw new ArgumentException("Text is required.", nameof(request));

        string documentId = request.DocumentId.Trim();
        string title = request.Title.Trim();

        IReadOnlyList<string> chunks = chunker.Split(request.Text);
        if (chunks.Count == 0)
            throw new ArgumentException("The document did not produce any chunks.", nameof(request));

        // Generate and validate every embedding before replacing the currently
        // stored version of the document. A provider failure therefore cannot
        // erase the existing searchable document.
        var replacement = new List<DocumentChunk>(chunks.Count);

        for (int index = 0; index < chunks.Count; index++)
        {
            ReadOnlyMemory<float> vector = await embeddings.GenerateVectorAsync(chunks[index]);
            ValidateVector(vector);

            replacement.Add(new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                ChunkNumber = index + 1,
                Title = title,
                Text = chunks[index],
                EmbeddingModel = embeddingOptions.Value.Model,
                Embedding = new Vector(vector.ToArray())
            });
        }

        // Replace the old chunks atomically after all embeddings are ready.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.DocumentChunks
            .Where(x => x.DocumentId == documentId)
            .ExecuteDeleteAsync(cancellationToken);

        db.DocumentChunks.AddRange(replacement);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new IngestDocumentResponse(
            documentId,
            chunks.Count,
            embeddingOptions.Value.Model,
            EmbeddingDimensions.Value);
    }

    private static void ValidateVector(ReadOnlyMemory<float> vector)
    {
        if (vector.Length != EmbeddingDimensions.Value)
        {
            throw new InvalidOperationException(
                $"Embedding dimension mismatch. The schema expects {EmbeddingDimensions.Value}, " +
                $"but the provider returned {vector.Length}. Rebuild the vector schema before " +
                "switching to a model with a different embedding size.");
        }

        if (vector.Span.ToArray().Any(x => float.IsNaN(x) || float.IsInfinity(x)))
        {
            throw new InvalidOperationException("The embedding provider returned invalid numeric values.");
        }
    }
}
