using EfCoreProviderContrastMinimal.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EfCoreProviderContrastMinimal.Providers;

public sealed class
    SqliteTestDatabase :
    IAsyncDisposable
{
    private readonly
        SqliteConnection
        _connection;

    private readonly
        DbContextOptions<
            ProviderDbContext>
        _options;

    private SqliteTestDatabase(
        SqliteConnection connection,
        DbContextOptions<
            ProviderDbContext>
            options)
    {
        _connection =
            connection;

        _options =
            options;
    }

    public static async Task<
        SqliteTestDatabase>
        CreateAsync(
            CancellationToken
                cancellationToken =
                    default)
    {
        var connection =
            new SqliteConnection(
                "Data Source=:memory:;Foreign Keys=True");

        await connection
            .OpenAsync(
                cancellationToken);

        try
        {
            DbContextOptions<
                ProviderDbContext>
                options =
                    new DbContextOptionsBuilder<
                        ProviderDbContext>()
                    .UseSqlite(
                        connection)
                    .EnableDetailedErrors()
                    .Options;

            var database =
                new SqliteTestDatabase(
                    connection,
                    options);

            await using
                ProviderDbContext context =
                    database
                        .CreateContext();

            await context.Database
                .EnsureCreatedAsync(
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

    public ProviderDbContext
        CreateContext() =>
            new(
                _options);

    public ValueTask
        DisposeAsync() =>
            _connection
                .DisposeAsync();
}