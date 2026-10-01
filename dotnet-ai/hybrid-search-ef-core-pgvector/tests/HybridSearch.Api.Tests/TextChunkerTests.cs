using HybridSearch.Api.Services;

namespace HybridSearch.Api.Tests;

public sealed class TextChunkerTests
{
    private readonly TextChunker _chunker = new();

    [Fact]
    public void Short_text_produces_one_chunk()
    {
        IReadOnlyList<string> chunks = _chunker.Split("one two three", maxWords: 10, overlapWords: 2);
        Assert.Single(chunks);
        Assert.Equal("one two three", chunks[0]);
    }

    [Fact]
    public void Chunking_preserves_requested_overlap()
    {
        IReadOnlyList<string> chunks = _chunker.Split(
            "one two three four five six seven",
            maxWords: 4,
            overlapWords: 2);

        Assert.Equal(3, chunks.Count);
        Assert.Equal("one two three four", chunks[0]);
        Assert.Equal("three four five six", chunks[1]);
        Assert.Equal("five six seven", chunks[2]);
    }

    [Fact]
    public void Empty_text_returns_no_chunks()
    {
        Assert.Empty(_chunker.Split("   "));
    }

    [Fact]
    public void Overlap_must_be_smaller_than_chunk_size()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _chunker.Split("one two", maxWords: 2, overlapWords: 2));
    }
}