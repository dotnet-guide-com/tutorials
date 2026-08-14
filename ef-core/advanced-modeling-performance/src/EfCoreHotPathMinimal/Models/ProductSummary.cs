namespace EfCoreHotPathMinimal.Models;

public sealed record ProductSummary(
    string Sku,
    string Name,
    int PriceCents,
    int StockQuantity);