using EfCoreRelationshipsMinimal.Data;
using EfCoreRelationshipsMinimal.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreRelationshipsMinimal.Services;

public sealed record
    GraphCounts(
        int Categories,
        int Todos,
        int Tags);

public sealed record
    NPlusOneResult(
        int TodoCount,
        int SelectCount);

public sealed record
    IncludeResult(
        int TodoCount,
        IReadOnlyList<
            string> CategoryNames,
        int SelectCount,
        int TrackedEntries);

public sealed record
    FilteredIncludeResult(
        string TodoTitle,
        IReadOnlyList<
            string> TagNames,
        int SelectCount);

public sealed record
    SplitGraphResult(
        int CategoryCount,
        int TodoCount,
        int TagCount,
        int SelectCount);

public sealed record
    ExplicitLoadResult(
        string TodoTitle,
        string CategoryName,
        bool WasLoadedBefore,
        bool IsLoadedAfter,
        int SelectCount);

public sealed record
    LoadingWorkflowResult(
        GraphCounts SeededGraph,
        NPlusOneResult
            NPlusOne,
        IncludeResult
            EagerInclude,
        SplitGraphResult
            SplitGraph,
        ExplicitLoadResult
            ExplicitLoad);

public static class LoadingScenarios
{
    public static async Task<
        GraphCounts>
        CountSeededGraphAsync(
            RelationshipDatabase
                database,
            CancellationToken
                cancellationToken =
                    default)
    {
        await using
            RelationshipDbContext context =
                database
                    .CreateContext();

        int categories =
            await context.Categories
                .CountAsync(
                    cancellationToken);

        int todos =
            await context.Todos
                .CountAsync(
                    cancellationToken);

        int tags =
            await context.Tags
                .CountAsync(
                    cancellationToken);

        return new GraphCounts(
            categories,
            todos,
            tags);
    }

    public static async Task<
        NPlusOneResult>
        RunNPlusOneAsync(
            RelationshipDatabase
                database,
            CancellationToken
                cancellationToken =
                    default)
    {
        database
            .SelectCounter
            .Reset();

        await using
            RelationshipDbContext context =
                database
                    .CreateContext();

        List<TodoItem> todos =
            await context.Todos
                .AsNoTracking()
                .OrderBy(
                    todo =>
                        todo.Id)
                .ToListAsync(
                    cancellationToken);

        foreach (
            TodoItem todo
            in todos)
        {
            _ =
                await context.Categories
                    .AsNoTracking()
                    .Where(
                        category =>
                            category.Id
                                == todo.CategoryId)
                    .Select(
                        category =>
                            category.Name)
                    .SingleAsync(
                        cancellationToken);
        }

        return new NPlusOneResult(
            TodoCount:
                todos.Count,

            SelectCount:
                database
                    .SelectCounter
                    .SelectCount);
    }

    public static async Task<
        IncludeResult>
        RunEagerIncludeAsync(
            RelationshipDatabase
                database,
            CancellationToken
                cancellationToken =
                    default)
    {
        database
            .SelectCounter
            .Reset();

        await using
            RelationshipDbContext context =
                database
                    .CreateContext();

        List<TodoItem> todos =
            await context.Todos
                .AsNoTracking()
                .Include(
                    todo =>
                        todo.Category)
                .OrderBy(
                    todo =>
                        todo.Id)
                .ToListAsync(
                    cancellationToken);

        return new IncludeResult(
            TodoCount:
                todos.Count,

            CategoryNames:
                todos
                    .Select(
                        todo =>
                            todo.Category.Name)
                    .ToArray(),

            SelectCount:
                database
                    .SelectCounter
                    .SelectCount,

            TrackedEntries:
                context
                    .ChangeTracker
                    .Entries()
                    .Count());
    }

    public static async Task<
        FilteredIncludeResult>
        RunFilteredIncludeAsync(
            RelationshipDatabase
                database,
            CancellationToken
                cancellationToken =
                    default)
    {
        database
            .SelectCounter
            .Reset();

        await using
            RelationshipDbContext context =
                database
                    .CreateContext();

        TodoItem todo =
            await context.Todos
                .AsNoTracking()
                .Include(
                    item =>
                        item.Tags
                            .Where(
                                tag =>
                                    tag.Name
                                        == "urgent"))
                .SingleAsync(
                    item =>
                        item.Id
                            == 1,
                    cancellationToken);

        return new FilteredIncludeResult(
            TodoTitle:
                todo.Title,

            TagNames:
                todo.Tags
                    .Select(
                        tag =>
                            tag.Name)
                    .OrderBy(
                        name =>
                            name,
                        StringComparer
                            .Ordinal)
                    .ToArray(),

            SelectCount:
                database
                    .SelectCounter
                    .SelectCount);
    }

    public static async Task<
        SplitGraphResult>
        RunSplitGraphAsync(
            RelationshipDatabase
                database,
            CancellationToken
                cancellationToken =
                    default)
    {
        database
            .SelectCounter
            .Reset();

        await using
            RelationshipDbContext context =
                database
                    .CreateContext();

        List<Category> categories =
            await context.Categories
                .AsNoTracking()
                .Include(
                    category =>
                        category.Todos)
                .ThenInclude(
                    todo =>
                        todo.Tags)
                .AsSplitQuery()
                .OrderBy(
                    category =>
                        category.Id)
                .ToListAsync(
                    cancellationToken);

        TodoItem[] todos =
            categories
                .SelectMany(
                    category =>
                        category.Todos)
                .DistinctBy(
                    todo =>
                        todo.Id)
                .ToArray();

        int tagCount =
            todos
                .SelectMany(
                    todo =>
                        todo.Tags)
                .DistinctBy(
                    tag =>
                        tag.Id)
                .Count();

        return new SplitGraphResult(
            CategoryCount:
                categories.Count,

            TodoCount:
                todos.Length,

            TagCount:
                tagCount,

            SelectCount:
                database
                    .SelectCounter
                    .SelectCount);
    }

    public static async Task<
        ExplicitLoadResult>
        RunExplicitReferenceLoadAsync(
            RelationshipDatabase
                database,
            CancellationToken
                cancellationToken =
                    default)
    {
        database
            .SelectCounter
            .Reset();

        await using
            RelationshipDbContext context =
                database
                    .CreateContext();

        TodoItem todo =
            await context.Todos
                .SingleAsync(
                    item =>
                        item.Id
                            == 1,
                    cancellationToken);

        var reference =
            context
                .Entry(
                    todo)
                .Reference(
                    item =>
                        item.Category);

        bool before =
            reference.IsLoaded;

        await reference
            .LoadAsync(
                cancellationToken);

        bool after =
            reference.IsLoaded;

        return new ExplicitLoadResult(
            TodoTitle:
                todo.Title,

            CategoryName:
                todo.Category.Name,

            WasLoadedBefore:
                before,

            IsLoadedAfter:
                after,

            SelectCount:
                database
                    .SelectCounter
                    .SelectCount);
    }

    public static async Task<
        LoadingWorkflowResult>
        RunAsync(
            CancellationToken
                cancellationToken =
                    default)
    {
        await using
            RelationshipDatabase database =
                await RelationshipDatabase
                    .CreateAsync(
                        cancellationToken);

        GraphCounts seeded =
            await CountSeededGraphAsync(
                database,
                cancellationToken);

        NPlusOneResult nPlusOne =
            await RunNPlusOneAsync(
                database,
                cancellationToken);

        IncludeResult eager =
            await RunEagerIncludeAsync(
                database,
                cancellationToken);

        SplitGraphResult split =
            await RunSplitGraphAsync(
                database,
                cancellationToken);

        ExplicitLoadResult explicitLoad =
            await RunExplicitReferenceLoadAsync(
                database,
                cancellationToken);

        return new LoadingWorkflowResult(
            SeededGraph:
                seeded,

            NPlusOne:
                nPlusOne,

            EagerInclude:
                eager,

            SplitGraph:
                split,

            ExplicitLoad:
                explicitLoad);
    }
}