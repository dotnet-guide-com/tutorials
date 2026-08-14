using EfCoreHotPathMinimal.Data;
using EfCoreHotPathMinimal.Models;
using EfCoreHotPathMinimal.Queries;
using EfCoreHotPathMinimal.Services;
using Microsoft.EntityFrameworkCore;

namespace EfCoreHotPathMinimal.Tests;

public sealed class EfCoreHotPathTests
{
    [Fact]
    public async Task
        Global_filter_excludes_soft_deleted_rows()
    {
        CancellationToken token =
            TestContext.Current
                .CancellationToken;

        await using
            CatalogDatabase database =
                await CatalogDatabase
                    .CreateAsync(
                        token);

        await using
            CatalogDbContext context =
                database
                    .CreateContext();

        int visible =
            await context.Products
                .CountAsync(
                    token);

        int all =
            await context.Products
                .IgnoreQueryFilters(
                    [
                        CatalogDbContext
                            .SoftDeleteFilterName
                    ])
                .CountAsync(
                    token);

        Assert.Equal(
            4,
            visible);

        Assert.Equal(
            5,
            all);
    }

    [Fact]
    public async Task
        Read_projection_does_not_track_entities()
    {
        CancellationToken token =
            TestContext.Current
                .CancellationToken;

        await using
            CatalogDatabase database =
                await CatalogDatabase
                    .CreateAsync(
                        token);

        await using
            CatalogDbContext context =
                database
                    .CreateContext();

        List<ProductSummary>
            products =
                await CatalogQueries
                    .ListVisibleAsync(
                        context,
                        token);

        Assert.Equal(
            4,
            products.Count);

        Assert.Empty(
            context.ChangeTracker
                .Entries());
    }

    [Fact]
    public async Task
        Compiled_lookup_returns_projected_product()
    {
        CancellationToken token =
            TestContext.Current
                .CancellationToken;

        await using
            CatalogDatabase database =
                await CatalogDatabase
                    .CreateAsync(
                        token);

        await using
            CatalogDbContext context =
                database
                    .CreateContext();

        ProductSummary? product =
            await CatalogQueries
                .FindBySkuAsync(
                    context,
                    "SKU-003",
                    token);

        Assert.NotNull(
            product);

        Assert.Equal(
            "Dock",
            product.Name);

        Assert.Equal(
            12_900,
            product.PriceCents);

        Assert.Equal(
            1,
            product.StockQuantity);

        Assert.Empty(
            context.ChangeTracker
                .Entries());
    }

    [Fact]
    public async Task
        Compiled_lookup_respects_soft_delete_filter()
    {
        CancellationToken token =
            TestContext.Current
                .CancellationToken;

        await using
            CatalogDatabase database =
                await CatalogDatabase
                    .CreateAsync(
                        token);

        await using
            CatalogDbContext context =
                database
                    .CreateContext();

        ProductSummary? product =
            await CatalogQueries
                .FindBySkuAsync(
                    context,
                    "SKU-004",
                    token);

        Assert.Null(
            product);

        bool rowExists =
            await context.Products
                .IgnoreQueryFilters(
                    [
                        CatalogDbContext
                            .SoftDeleteFilterName
                    ])
                .AnyAsync(
                    candidate =>
                        candidate.Sku
                            == "SKU-004",
                    token);

        Assert.True(
            rowExists);
    }

    [Fact]
    public async Task
        ExecuteUpdate_updates_matching_rows_and_returns_count()
    {
        CancellationToken token =
            TestContext.Current
                .CancellationToken;

        await using
            CatalogDatabase database =
                await CatalogDatabase
                    .CreateAsync(
                        token);

        await using
            CatalogDbContext context =
                database
                    .CreateContext();

        int updated =
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
                    token);

        Assert.Equal(
            3,
            updated);

        await using
            CatalogDbContext verification =
                database
                    .CreateContext();

        Dictionary<
            string,
            int>
            stock =
                await verification
                    .Products
                    .AsNoTracking()
                    .ToDictionaryAsync(
                        product =>
                            product.Sku,
                        product =>
                            product.StockQuantity,
                        token);

        Assert.Equal(
            7,
            stock["SKU-001"]);

        Assert.Equal(
            8,
            stock["SKU-002"]);

        Assert.Equal(
            6,
            stock["SKU-003"]);

        Assert.Equal(
            9,
            stock["SKU-005"]);
    }

    [Fact]
    public async Task
        ExecuteUpdate_does_not_synchronize_pretracked_entity()
    {
        CancellationToken token =
            TestContext.Current
                .CancellationToken;

        await using
            CatalogDatabase database =
                await CatalogDatabase
                    .CreateAsync(
                        token);

        await using
            CatalogDbContext context =
                database
                    .CreateContext();

        Product tracked =
            await context.Products
                .SingleAsync(
                    product =>
                        product.Sku
                            == "SKU-001",
                    token);

        Assert.Equal(
            2,
            tracked.StockQuantity);

        int updated =
            await context.Products
                .Where(
                    product =>
                        product.Sku
                            == "SKU-001")
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(
                            product =>
                                product.StockQuantity,
                            product =>
                                product.StockQuantity
                                    + 10),
                    token);

        Assert.Equal(
            1,
            updated);

        Assert.Equal(
            2,
            tracked.StockQuantity);

        await using
            CatalogDbContext verification =
                database
                    .CreateContext();

        int stored =
            await verification
                .Products
                .AsNoTracking()
                .Where(
                    product =>
                        product.Sku
                            == "SKU-001")
                .Select(
                    product =>
                        product.StockQuantity)
                .SingleAsync(
                    token);

        Assert.Equal(
            12,
            stored);
    }

    [Fact]
    public async Task
        ExecuteDelete_can_purge_soft_deleted_rows_explicitly()
    {
        CancellationToken token =
            TestContext.Current
                .CancellationToken;

        await using
            CatalogDatabase database =
                await CatalogDatabase
                    .CreateAsync(
                        token);

        await using
            CatalogDbContext context =
                database
                    .CreateContext();

        int deleted =
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
                    token);

        Assert.Equal(
            1,
            deleted);

        await using
            CatalogDbContext verification =
                database
                    .CreateContext();

        int all =
            await verification
                .Products
                .IgnoreQueryFilters(
                    [
                        CatalogDbContext
                            .SoftDeleteFilterName
                    ])
                .CountAsync(
                    token);

        Assert.Equal(
            4,
            all);
    }

    [Fact]
    public async Task
        Workflow_returns_deterministic_summary()
    {
        CatalogWorkflowResult result =
            await CatalogWorkflow
                .RunAsync(
                    TestContext.Current
                        .CancellationToken);

        Assert.Equal(
            4,
            result.VisibleProducts);

        Assert.Equal(
            "SKU-003",
            result.CompiledLookup.Sku);

        Assert.Equal(
            "Dock",
            result.CompiledLookup.Name);

        Assert.Equal(
            3,
            result.RestockedProducts);

        Assert.Equal(
            1,
            result.PurgedProducts);

        Assert.Equal(
            4,
            result.FinalRows);
    }
}