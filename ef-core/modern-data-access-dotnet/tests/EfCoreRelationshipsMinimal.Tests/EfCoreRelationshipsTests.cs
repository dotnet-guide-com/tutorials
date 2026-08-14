using EfCoreRelationshipsMinimal.Data;
using EfCoreRelationshipsMinimal.Models;
using EfCoreRelationshipsMinimal.Services;
using Microsoft.EntityFrameworkCore;

namespace EfCoreRelationshipsMinimal.Tests;

public sealed class
    EfCoreRelationshipsTests
{
    [Fact]
    public async Task
        Seeded_graph_has_expected_relationship_counts()
    {
        CancellationToken token =
            TestContext.Current
                .CancellationToken;

        await using
            RelationshipDatabase database =
                await RelationshipDatabase
                    .CreateAsync(
                        token);

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
                    token);

        TodoItem[] todos =
            categories
                .SelectMany(
                    category =>
                        category.Todos)
                .DistinctBy(
                    todo =>
                        todo.Id)
                .ToArray();

        Tag[] tags =
            todos
                .SelectMany(
                    todo =>
                        todo.Tags)
                .DistinctBy(
                    tag =>
                        tag.Id)
                .ToArray();

        Assert.Equal(
            2,
            categories.Count);

        Assert.Equal(
            3,
            todos.Length);

        Assert.Equal(
            3,
            tags.Length);
    }

    [Fact]
    public async Task
        Manual_category_lookup_demonstrates_n_plus_one()
    {
        CancellationToken token =
            TestContext.Current
                .CancellationToken;

        await using
            RelationshipDatabase database =
                await RelationshipDatabase
                    .CreateAsync(
                        token);

        NPlusOneResult result =
            await LoadingScenarios
                .RunNPlusOneAsync(
                    database,
                    token);

        Assert.Equal(
            3,
            result.TodoCount);

        Assert.Equal(
            4,
            result.SelectCount);
    }

    [Fact]
    public async Task
        Eager_include_loads_categories_in_one_select()
    {
        CancellationToken token =
            TestContext.Current
                .CancellationToken;

        await using
            RelationshipDatabase database =
                await RelationshipDatabase
                    .CreateAsync(
                        token);

        IncludeResult result =
            await LoadingScenarios
                .RunEagerIncludeAsync(
                    database,
                    token);

        Assert.Equal(
            3,
            result.TodoCount);

        Assert.Equal(
            [
                "Work",
                "Work",
                "Personal"
            ],
            result.CategoryNames);

        Assert.Equal(
            1,
            result.SelectCount);
    }

    [Fact]
    public async Task
        Filtered_include_loads_only_requested_tag()
    {
        CancellationToken token =
            TestContext.Current
                .CancellationToken;

        await using
            RelationshipDatabase database =
                await RelationshipDatabase
                    .CreateAsync(
                        token);

        FilteredIncludeResult result =
            await LoadingScenarios
                .RunFilteredIncludeAsync(
                    database,
                    token);

        Assert.Equal(
            "Prepare release",
            result.TodoTitle);

        Assert.Equal(
            ["urgent"],
            result.TagNames);

        Assert.Equal(
            1,
            result.SelectCount);
    }

    [Fact]
    public async Task
        Split_query_loads_complete_graph_in_three_selects()
    {
        CancellationToken token =
            TestContext.Current
                .CancellationToken;

        await using
            RelationshipDatabase database =
                await RelationshipDatabase
                    .CreateAsync(
                        token);

        SplitGraphResult result =
            await LoadingScenarios
                .RunSplitGraphAsync(
                    database,
                    token);

        Assert.Equal(
            2,
            result.CategoryCount);

        Assert.Equal(
            3,
            result.TodoCount);

        Assert.Equal(
            3,
            result.TagCount);

        Assert.Equal(
            3,
            result.SelectCount);
    }

    [Fact]
    public async Task
        Explicit_reference_load_is_conditional_and_two_selects()
    {
        CancellationToken token =
            TestContext.Current
                .CancellationToken;

        await using
            RelationshipDatabase database =
                await RelationshipDatabase
                    .CreateAsync(
                        token);

        ExplicitLoadResult result =
            await LoadingScenarios
                .RunExplicitReferenceLoadAsync(
                    database,
                    token);

        Assert.Equal(
            "Prepare release",
            result.TodoTitle);

        Assert.Equal(
            "Work",
            result.CategoryName);

        Assert.False(
            result.WasLoadedBefore);

        Assert.True(
            result.IsLoadedAfter);

        Assert.Equal(
            2,
            result.SelectCount);
    }

    [Fact]
    public async Task
        No_tracking_include_leaves_change_tracker_empty()
    {
        CancellationToken token =
            TestContext.Current
                .CancellationToken;

        await using
            RelationshipDatabase database =
                await RelationshipDatabase
                    .CreateAsync(
                        token);

        IncludeResult result =
            await LoadingScenarios
                .RunEagerIncludeAsync(
                    database,
                    token);

        Assert.Equal(
            0,
            result.TrackedEntries);
    }

    [Fact]
    public async Task
        Workflow_returns_deterministic_summary()
    {
        LoadingWorkflowResult result =
            await LoadingScenarios
                .RunAsync(
                    TestContext.Current
                        .CancellationToken);

        Assert.Equal(
            new GraphCounts(
                2,
                3,
                3),
            result.SeededGraph);

        Assert.Equal(
            4,
            result.NPlusOne
                .SelectCount);

        Assert.Equal(
            1,
            result.EagerInclude
                .SelectCount);

        Assert.Equal(
            3,
            result.SplitGraph
                .SelectCount);

        Assert.Equal(
            2,
            result.ExplicitLoad
                .SelectCount);
    }
}