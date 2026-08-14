using EfCoreRelationshipsMinimal.Diagnostics;
using EfCoreRelationshipsMinimal.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EfCoreRelationshipsMinimal.Data;

public sealed class RelationshipDatabase :
    IAsyncDisposable
{
    private readonly
        SqliteConnection
        _connection;

    private readonly
        DbContextOptions<
            RelationshipDbContext>
        _options;

    private RelationshipDatabase(
        SqliteConnection connection,
        SelectCountingInterceptor
            selectCounter,
        DbContextOptions<
            RelationshipDbContext>
            options)
    {
        _connection =
            connection;

        SelectCounter =
            selectCounter;

        _options =
            options;
    }

    public SelectCountingInterceptor
        SelectCounter
    {
        get;
    }

    public static async Task<
        RelationshipDatabase>
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
            var selectCounter =
                new SelectCountingInterceptor();

            DbContextOptions<
                RelationshipDbContext>
                options =
                    new DbContextOptionsBuilder<
                        RelationshipDbContext>()
                    .UseSqlite(
                        connection)
                    .AddInterceptors(
                        selectCounter)
                    .EnableDetailedErrors()
                    .Options;

            var database =
                new RelationshipDatabase(
                    connection,
                    selectCounter,
                    options);

            await using
                RelationshipDbContext context =
                    database
                        .CreateContext();

            await context.Database
                .EnsureCreatedAsync(
                    cancellationToken);

            await SeedAsync(
                context,
                cancellationToken);

            selectCounter.Reset();

            return database;
        }
        catch
        {
            await connection
                .DisposeAsync();

            throw;
        }
    }

    public RelationshipDbContext
        CreateContext() =>
            new(
                _options);

    public ValueTask
        DisposeAsync() =>
            _connection
                .DisposeAsync();

    private static async Task
        SeedAsync(
            RelationshipDbContext
                context,
            CancellationToken
                cancellationToken)
    {
        var work =
            new Category
            {
                Id = 1,
                Name = "Work"
            };

        var personal =
            new Category
            {
                Id = 2,
                Name = "Personal"
            };

        var urgent =
            new Tag
            {
                Id = 1,
                Name = "urgent"
            };

        var planning =
            new Tag
            {
                Id = 2,
                Name = "planning"
            };

        var home =
            new Tag
            {
                Id = 3,
                Name = "home"
            };

        var release =
            new TodoItem
            {
                Id = 1,
                Title =
                    "Prepare release",
                Category =
                    work
            };

        release.Tags.Add(
            urgent);

        release.Tags.Add(
            planning);

        var metrics =
            new TodoItem
            {
                Id = 2,
                Title =
                    "Review metrics",
                Category =
                    work
            };

        metrics.Tags.Add(
            planning);

        var dentist =
            new TodoItem
            {
                Id = 3,
                Title =
                    "Book dentist",
                Category =
                    personal
            };

        dentist.Tags.Add(
            home);

        context.AddRange(
            work,
            personal,
            urgent,
            planning,
            home,
            release,
            metrics,
            dentist);

        await context
            .SaveChangesAsync(
                cancellationToken);
    }
}