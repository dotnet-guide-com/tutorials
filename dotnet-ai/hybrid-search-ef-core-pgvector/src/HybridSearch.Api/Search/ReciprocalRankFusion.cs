using HybridSearch.Api.Models;

namespace HybridSearch.Api.Search;

public sealed record RankedCandidate(
    Guid Id,
    string DocumentId,
    int ChunkNumber,
    string Title,
    string Text,
    int Rank,
    double SourceScore);

public static class ReciprocalRankFusion
{
    public static IReadOnlyList<SearchHit> Fuse(
        IReadOnlyList<RankedCandidate> keyword,
        IReadOnlyList<RankedCandidate> vector,
        int top,
        int k = 60,
        double keywordWeight = 1.0,
        double vectorWeight = 1.0)
    {
        if (top <= 0) throw new ArgumentOutOfRangeException(nameof(top));
        if (k < 0) throw new ArgumentOutOfRangeException(nameof(k));
        if (keywordWeight < 0) throw new ArgumentOutOfRangeException(nameof(keywordWeight));
        if (vectorWeight < 0) throw new ArgumentOutOfRangeException(nameof(vectorWeight));

        var accumulator = new Dictionary<Guid, MutableResult>();
        Add(keyword, keywordWeight, isKeyword: true);
        Add(vector, vectorWeight, isKeyword: false);

        return accumulator.Values
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Id)
            .Take(top)
            .Select(x => new SearchHit(
                x.Id,
                x.DocumentId,
                x.ChunkNumber,
                x.Title,
                x.Text,
                x.Score,
                x.KeywordRank,
                x.VectorRank))
            .ToList();

        void Add(IReadOnlyList<RankedCandidate> source, double weight, bool isKeyword)
        {
            foreach (RankedCandidate candidate in source)
            {
                if (candidate.Rank <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(source),
                        "Ranks must start at 1.");
                }

                if (!accumulator.TryGetValue(candidate.Id, out MutableResult? item))
                {
                    item = new MutableResult(candidate);
                    accumulator.Add(candidate.Id, item);
                }

                item.Score += weight / (k + candidate.Rank);
                if (isKeyword) item.KeywordRank = candidate.Rank;
                else item.VectorRank = candidate.Rank;
            }
        }
    }

    private sealed class MutableResult(RankedCandidate candidate)
    {
        public Guid Id { get; } = candidate.Id;
        public string DocumentId { get; } = candidate.DocumentId;
        public int ChunkNumber { get; } = candidate.ChunkNumber;
        public string Title { get; } = candidate.Title;
        public string Text { get; } = candidate.Text;
        public double Score { get; set; }
        public int? KeywordRank { get; set; }
        public int? VectorRank { get; set; }
    }
}