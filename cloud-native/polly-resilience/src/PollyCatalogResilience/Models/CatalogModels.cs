namespace PollyCatalogResilience.Models;

public enum CatalogSimulationMode
{
    Live,
    Failure,
    Slow,
    Hold
}

public sealed record CatalogProduct(
    int Id,
    string Name,
    decimal Price);

public sealed record CatalogSnapshot(
    DateTimeOffset GeneratedAtUtc,
    string Source,
    bool IsStale,
    string? DegradedReason,
    CatalogProduct[] Products);

public sealed record ResilienceStatus(
    long Fallbacks,
    long Timeouts,
    long Rejections,
    string LastFallbackReason);