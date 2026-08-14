using EfCoreHotPathMinimal.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EfCoreHotPathMinimal.Data;

public sealed class CatalogDatabase :
    IAsyncDisposable
{
    private readonly SqliteConnection
        _connection;

    private readonly
        DbContextOptions<
            CatalogDbContext>
        _options;

    private CatalogDatabase(
        SqliteConnection connection,
        DbContextOptions<
            CatalogDbContext>
            options)
    {
        _connection =
            connection;

        _options =
            options;
    }

    public static async Task<
        CatalogDatabase>
        CreateAsync(
            CancellationToken
                cancellationToken =
                    default)
    {
        var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection
            .OpenAsync(
                cancellationToken);

        try
        {
            DbContextOptions<
                CatalogDbContext>
                options =
                    new DbContextOptionsBuilder<
                        CatalogDbContext>()
                    .UseSqlite(
                        connection)
                    .EnableDetailedErrors()
                    .Options;

            var database =
                new CatalogDatabase(
                    connection,
                    options);

            await using
                CatalogDbContext context =
                    database
                        .CreateContext();

            await context.Database
                .EnsureCreatedAsync(
                    cancellationToken);

            context.Products.AddRange(
                new Product
                {
                    Id = 1,
                    Sku = "SKU-001",
                    Name = "Keyboard",
                    PriceCents = 7_500,
                    StockQuantity = 2
                },
                new Product
                {
                    Id = 2,
                    Sku = "SKU-002",
                    Name = "Mouse",
                    PriceCents = 3_500,
                    StockQuantity = 8
                },
                new Product
                {
                    Id = 3,
                    Sku = "SKU-003",
                    Name = "Dock",
                    PriceCents = 12_900,
                    StockQuantity = 1
                },
                new Product
                {
                    Id = 4,
                    Sku = "SKU-004",
                    Name = "Webcam",
                    PriceCents = 6_500,
                    StockQuantity = 0,
                    IsDeleted = true
                },
                new Product
                {
                    Id = 5,
                    Sku = "SKU-005",
                    Name = "Stand",
                    PriceCents = 4_500,
                    StockQuantity = 4
                });

            await context
                .SaveChangesAsync(
                    cancellationToken);

            return database;
        }
        catch
        {
            await connection
                .DisposeAsync();

            throw;
        }
    }

    public CatalogDbContext
        CreateContext() =>
            new(
                _options);

    public ValueTask
        DisposeAsync() =>
            _connection
                .DisposeAsync();
}