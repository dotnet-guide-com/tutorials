var documents = new[]
{
    new SearchDocument(
        "doc-1",
        "Graceful shutdown with CancellationToken"),

    new SearchDocument(
        "doc-2",
        "Stopping hosted services safely"),

    new SearchDocument(
        "doc-3",
        "BackgroundService lifecycle"),

    new SearchDocument(
        "doc-4",
        "Dependency injection fundamentals")
};

var keywordResults = new[]
{
    new RankedCandidate(
        documents[0],
        Rank: 1,
        SourceScore: 8.20),

    new RankedCandidate(
        documents[2],
        Rank: 2,
        SourceScore: 6.10),

    new RankedCandidate(
        documents[1],
        Rank: 3,
        SourceScore: 4.90)
};

var vectorResults = new[]
{
    new RankedCandidate(
        documents[1],
        Rank: 1,
        SourceScore: 0.93),

    new RankedCandidate(
        documents[0],
        Rank: 2,
        SourceScore: 0.89),

    new RankedCandidate(
        documents[2],
        Rank: 3,
        SourceScore: 0.81)
};

IReadOnlyList<FusedResult> fusedResults =
    ReciprocalRankFusion.Fuse(
        keywordResults,
        vectorResults,
        top: 3,
        k: 60);

Console.WriteLine("HYBRID SEARCH — RECIPROCAL RANK FUSION");
Console.WriteLine();

foreach (FusedResult result in fusedResults)
{
    Console.WriteLine(
        $"{result.Document.Id} | " +
        $"{result.Document.Title}");

    Console.WriteLine(
        $"  Fused score : {result.Score:F6}");

    Console.WriteLine(
        $"  Keyword rank: " +
        $"{result.KeywordRank?.ToString() ?? "-"}");

    Console.WriteLine(
        $"  Vector rank : " +
        $"{result.VectorRank?.ToString() ?? "-"}");

    Console.WriteLine();
}

internal sealed record SearchDocument(
    string Id,
    string Title);

internal sealed record RankedCandidate(
    SearchDocument Document,
    int Rank,
    double SourceScore);

internal sealed record FusedResult(
    SearchDocument Document,
    double Score,
    int? KeywordRank,
    int? VectorRank);

internal static class ReciprocalRankFusion
{
    public static IReadOnlyList<FusedResult> Fuse(
        IReadOnlyList<RankedCandidate> keywordResults,
        IReadOnlyList<RankedCandidate> vectorResults,
        int top,
        int k = 60,
        double keywordWeight = 1.0,
        double vectorWeight = 1.0)
    {
        var results =
            new Dictionary<string, MutableResult>(
                StringComparer.Ordinal);

        AddResults(
            keywordResults,
            keywordWeight,
            isKeyword: true);

        AddResults(
            vectorResults,
            vectorWeight,
            isKeyword: false);

        return results.Values
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Document.Id)
            .Take(top)
            .Select(item => new FusedResult(
                item.Document,
                item.Score,
                item.KeywordRank,
                item.VectorRank))
            .ToList();

        void AddResults(
            IReadOnlyList<RankedCandidate> source,
            double weight,
            bool isKeyword)
        {
            foreach (RankedCandidate candidate in source)
            {
                if (!results.TryGetValue(
                        candidate.Document.Id,
                        out MutableResult? item))
                {
                    item = new MutableResult(
                        candidate.Document);

                    results.Add(
                        candidate.Document.Id,
                        item);
                }

                item.Score +=
                    weight / (k + candidate.Rank);

                if (isKeyword)
                {
                    item.KeywordRank =
                        candidate.Rank;
                }
                else
                {
                    item.VectorRank =
                        candidate.Rank;
                }
            }
        }
    }

    private sealed class MutableResult(
        SearchDocument document)
    {
        public SearchDocument Document { get; } =
            document;

        public double Score { get; set; }

        public int? KeywordRank { get; set; }

        public int? VectorRank { get; set; }
    }
}
