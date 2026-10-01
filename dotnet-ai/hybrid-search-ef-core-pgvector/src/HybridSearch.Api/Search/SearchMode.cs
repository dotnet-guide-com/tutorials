namespace HybridSearch.Api.Search;

public enum SearchMode
{
    Keyword,
    Vector,
    Hybrid
}

public static class SearchModeParser
{
    public static SearchMode Parse(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "keyword" => SearchMode.Keyword,
            "vector" => SearchMode.Vector,
            "hybrid" or null or "" => SearchMode.Hybrid,
            _ => throw new ArgumentException(
                "Search mode must be 'keyword', 'vector', or 'hybrid'.",
                nameof(value))
        };
}