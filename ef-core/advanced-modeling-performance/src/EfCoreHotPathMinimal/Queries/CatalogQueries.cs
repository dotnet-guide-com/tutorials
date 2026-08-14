using EfCoreHotPathMinimal.Data;
using EfCoreHotPathMinimal.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreHotPathMinimal.Queries;

public static class CatalogQueries
{
    private static readonly
        Func<
            CatalogDbContext,
            string,
            IAsyncEnumerable<
                ProductSummary>>
        FindBySkuCompiled =
            EF.CompileAsyncQuery(
                (
                    CatalogDbContext
                        context,
                    string sku) =>
                    context.Products
                        .AsNoTracking()
                        .Where(
                            product =>
                                product.Sku
                                    == sku)
                        .OrderBy(
                            product =>
                                product.Id)
                        .Select(
                            product =>
                                new ProductSummary(
                                    product.Sku,
                                    product.Name,
                                    product.PriceCents,
                                    product.StockQuantity))
                        .Take(
                            1));

    public static Task<
        List<ProductSummary>>
        ListVisibleAsync(
            CatalogDbContext
                context,
            CancellationToken
                cancellationToken =
                    default) =>
            context.Products
                .AsNoTracking()
                .OrderBy(
                    product =>
                        product.Id)
                .Select(
                    product =>
                        new ProductSummary(
                            product.Sku,
                            product.Name,
                            product.PriceCents,
                            product.StockQuantity))
                .ToListAsync(
                    cancellationToken);

    public static async Task<
        ProductSummary?>
        FindBySkuAsync(
            CatalogDbContext
                context,
            string sku,
            CancellationToken
                cancellationToken =
                    default)
    {
        await foreach (
            ProductSummary product
            in FindBySkuCompiled(
                    context,
                    sku)
                .WithCancellation(
                    cancellationToken))
        {
            return product;
        }

        return null;
    }
}