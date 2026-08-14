using EfCoreHotPathMinimal.Data;
using EfCoreHotPathMinimal.Models;
using EfCoreHotPathMinimal.Queries;
using Microsoft.EntityFrameworkCore;

namespace EfCoreHotPathMinimal.Services;

public sealed record
    CatalogWorkflowResult(
        int VisibleProducts,
        ProductSummary
            CompiledLookup,
        int RestockedProducts,
        int PurgedProducts,
        int FinalRows);

public static class CatalogWorkflow
{
    public static async Task<
        CatalogWorkflowResult>
        RunAsync(
            CancellationToken
                cancellationToken =
                    default)
    {
        await using
            CatalogDatabase database =
                await CatalogDatabase
                    .CreateAsync(
                        cancellationToken);

        await using
            CatalogDbContext context =
                database
                    .CreateContext();

        List<ProductSummary>
            visible =
                await CatalogQueries
                    .ListVisibleAsync(
                        context,
                        cancellationToken);

        ProductSummary lookup =
            await CatalogQueries
                .FindBySkuAsync(
                    context,
                    "SKU-003",
                    cancellationToken)
            ?? throw new InvalidOperationException(
                "Seeded SKU-003 was not found.");

        int restocked =
            await context.Products
                .Where(
                    product =>
                        product.StockQuantity
                            < 5)
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(
                            product =>
                                product.StockQuantity,
                            product =>
                                product.StockQuantity
                                    + 5),
                    cancellationToken);

        int purged =
            await context.Products
                .IgnoreQueryFilters(
                    [
                        CatalogDbContext
                            .SoftDeleteFilterName
                    ])
                .Where(
                    product =>
                        product.IsDeleted)
                .ExecuteDeleteAsync(
                    cancellationToken);

        int finalRows =
            await context.Products
                .IgnoreQueryFilters(
                    [
                        CatalogDbContext
                            .SoftDeleteFilterName
                    ])
                .CountAsync(
                    cancellationToken);

        return new CatalogWorkflowResult(
            VisibleProducts:
                visible.Count,

            CompiledLookup:
                lookup,

            RestockedProducts:
                restocked,

            PurgedProducts:
                purged,

            FinalRows:
                finalRows);
    }
}