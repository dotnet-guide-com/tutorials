using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace HybridSearch.Api.Data;

public sealed class SearchDbContext(DbContextOptions<SearchDbContext> options)
    : DbContext(options)
{
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.DocumentId, x.ChunkNumber }).IsUnique();

            entity.HasGeneratedTsVectorColumn(
                    x => x.SearchVector,
                    "english",
                    x => new { x.Title, x.Text })
                .HasIndex(x => x.SearchVector)
                .HasMethod("GIN");

            entity.HasIndex(x => x.Embedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops")
                .HasStorageParameter("m", 16)
                .HasStorageParameter("ef_construction", 64);
        });
    }
}