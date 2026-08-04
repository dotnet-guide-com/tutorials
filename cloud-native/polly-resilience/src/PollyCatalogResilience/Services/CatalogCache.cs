using PollyCatalogResilience.Models;

namespace PollyCatalogResilience.Services;

public sealed class CatalogCache
{
    private static readonly
        CatalogSnapshot CachedSnapshot =
            new(
                GeneratedAtUtc:
                    new DateTimeOffset(
                        2026,
                        8,
                        1,
                        0,
                        0,
                        0,
                        TimeSpan.Zero),

                Source:
                    "stale-cache",

                IsStale:
                    true,

                DegradedReason:
                    null,

                Products:
                [
                    new CatalogProduct(
                        1,
                        "Mechanical Keyboard",
                        89.00m),

                    new CatalogProduct(
                        2,
                        "USB-C Dock",
                        129.00m),

                    new CatalogProduct(
                        3,
                        "Monitor Arm",
                        79.00m)
                ]);

    public CatalogSnapshot CreateFallback(
        string reason) =>
            CachedSnapshot with
            {
                DegradedReason =
                    reason
            };
}