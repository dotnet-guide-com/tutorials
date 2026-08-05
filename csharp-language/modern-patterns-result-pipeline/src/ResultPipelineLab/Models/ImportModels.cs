namespace ResultPipelineLab.Models;

public sealed record RawProductRow(
    int LineNumber,
    string Name,
    string PriceText,
    string Category,
    string StockText);

public sealed record ValidatedProduct(
    int LineNumber,
    string Name,
    decimal Price,
    string Category,
    int Stock);

public sealed record ImportedProduct(
    string Name,
    decimal Price,
    string Category,
    int Stock);

public sealed record ImportSummary(
    int TotalInserted);