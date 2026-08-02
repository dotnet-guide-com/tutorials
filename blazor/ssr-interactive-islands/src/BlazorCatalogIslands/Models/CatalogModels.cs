namespace BlazorCatalogIslands.Models;

public sealed record ProductSummary(
    int Id,
    string Name,
    decimal Price,
    string Description);

public sealed record ReviewSummary(
    int Id,
    string Author,
    int Rating,
    string Comment);