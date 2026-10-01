using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;
using Pgvector;

namespace HybridSearch.Api.Data;

public sealed class DocumentChunk
{
    public Guid Id { get; init; }
    public string DocumentId { get; init; } = string.Empty;
    public int ChunkNumber { get; init; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;

    [Column(TypeName = "vector(768)")]
    public Vector? Embedding { get; set; }

    public NpgsqlTsVector SearchVector { get; private set; } = null!;
}