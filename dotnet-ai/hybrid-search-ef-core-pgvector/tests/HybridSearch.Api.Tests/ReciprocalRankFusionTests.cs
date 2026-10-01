using HybridSearch.Api.Search;

namespace HybridSearch.Api.Tests;

public sealed class ReciprocalRankFusionTests
{
    [Fact]
    public void Candidate_present_in_both_legs_can_rise_to_the_top()
    {
        Guid agreed = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid keywordOnly = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid vectorOnly = Guid.Parse("33333333-3333-3333-3333-333333333333");

        RankedCandidate[] keyword =
        [
            new(keywordOnly, "doc-k", 1, "Keyword", "...", 1, 9.0),
            new(agreed, "doc-a", 1, "Agreed", "...", 2, 7.0)
        ];

        RankedCandidate[] vector =
        [
            new(vectorOnly, "doc-v", 1, "Vector", "...", 1, 0.95),
            new(agreed, "doc-a", 1, "Agreed", "...", 2, 0.91)
        ];

        var result = ReciprocalRankFusion.Fuse(keyword, vector, top: 3, k: 60);

        Assert.Equal(agreed, result[0].Id);
        Assert.Equal(2, result[0].KeywordRank);
        Assert.Equal(2, result[0].VectorRank);
    }

    [Fact]
    public void Fusion_uses_rank_not_source_score_magnitude()
    {
        Guid first = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid second = Guid.Parse("22222222-2222-2222-2222-222222222222");

        RankedCandidate[] keyword =
        [
            new(first, "a", 1, "First", "...", 1, 0.0001),
            new(second, "b", 1, "Second", "...", 2, 999999.0)
        ];

        var result = ReciprocalRankFusion.Fuse(keyword, [], top: 2, k: 60);
        Assert.Equal(first, result[0].Id);
    }

    [Fact]
    public void Invalid_zero_rank_is_rejected()
    {
        RankedCandidate[] source =
        [
            new(Guid.NewGuid(), "bad", 1, "Bad", "...", 0, 1.0)
        ];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ReciprocalRankFusion.Fuse(source, [], top: 1));
    }

    [Fact]
    public void Top_limits_the_number_of_fused_results()
    {
        RankedCandidate[] keyword = Enumerable.Range(1, 5)
            .Select(i => new RankedCandidate(
                Guid.NewGuid(), $"doc-{i}", 1, $"Title {i}", "...", i, i))
            .ToArray();

        var result = ReciprocalRankFusion.Fuse(keyword, [], top: 2);
        Assert.Equal(2, result.Count);
    }
}