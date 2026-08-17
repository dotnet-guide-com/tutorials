using EfCoreProviderContrastMinimal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EfCoreProviderContrastMinimal.Providers;

public sealed class
    InMemoryTestDatabase :
    IAsyncDisposable
{
    private readonly
        InMemoryDatabaseRoot
        _root =
            new();

    private readonly
        string
        _databaseName =
            "EfCoreProviderContrast";

    private readonly
        DbContextOptions<
            ProviderDbContext>
        _options;

    public InMemoryTestDatabase()
    {
        _options =
            new DbContextOptionsBuilder<
                ProviderDbContext>()
                .UseInMemoryDatabase(
                    _databaseName,
                    _root)
                .EnableDetailedErrors()
                .Options;

        using
            ProviderDbContext context =
                CreateContext();

        context.Database
            .EnsureCreated();
    }

    public ProviderDbContext
        CreateContext() =>
            new(
                _options);

    public async ValueTask
        DisposeAsync()
    {
        await using
            ProviderDbContext context =
                CreateContext();

        await context.Database
            .EnsureDeletedAsync();
    }
}