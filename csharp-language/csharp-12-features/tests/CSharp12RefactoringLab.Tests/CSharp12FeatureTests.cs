using CSharp12RefactoringLab.Formatting;
using CSharp12RefactoringLab.Models;
using CSharp12RefactoringLab.Services;

namespace CSharp12RefactoringLab.Tests;

public sealed class CSharp12FeatureTests
{
    [Fact]
    public void
        Primary_constructor_initializes_explicit_properties()
    {
        var item =
            new TodoItem(
                "  Learn C# 12  ",
                isComplete:
                    true);

        Assert.Equal(
            "Learn C# 12",
            item.Title);

        Assert.True(
            item.IsComplete);

        Assert.Throws<
            ArgumentException>(
                () =>
                    new TodoItem(
                        "   "));
    }

    [Fact]
    public void
        Primary_constructor_parameters_are_not_public_properties()
    {
        Type itemType =
            typeof(
                TodoItem);

        Assert.Null(
            itemType.GetProperty(
                "title"));

        Assert.Null(
            itemType.GetProperty(
                "isComplete"));

        Assert.NotNull(
            itemType.GetProperty(
                nameof(
                    TodoItem.Title)));

        Assert.NotNull(
            itemType.GetProperty(
                nameof(
                    TodoItem.IsComplete)));
    }

    [Fact]
    public void
        Collection_expressions_copy_seed_and_snapshot_data()
    {
        TodoItem[] seed =
        [
            new TodoItem(
                "First")
        ];

        var service =
            new TodoService(
                seed);

        seed[0] =
            new TodoItem(
                "Changed outside");

        TodoItem[] firstSnapshot =
            service.Snapshot();

        firstSnapshot[0] =
            new TodoItem(
                "Changed snapshot");

        TodoItem[] secondSnapshot =
            service.Snapshot();

        Assert.Equal(
            "First",
            secondSnapshot[0]
                .Title);
    }

    [Fact]
    public void
        Spread_expression_prepends_welcome_without_mutating_service()
    {
        var service =
            new TodoService(
            [
                new TodoItem(
                    "Stored item")
            ]);

        TodoItem[] composed =
            service.GetAllWithWelcome();

        TodoItem[] stored =
            service.Snapshot();

        Assert.Equal(
            2,
            composed.Length);

        Assert.Equal(
            "Welcome to the C# 12 lab",
            composed[0].Title);

        Assert.Single(
            stored);

        Assert.Equal(
            "Stored item",
            stored[0].Title);
    }

    [Fact]
    public void
        Tuple_alias_returns_named_counts()
    {
        var service =
            new TodoService(
            [
                new TodoItem(
                    "Active"),

                new TodoItem(
                    "Completed",
                    isComplete:
                        true),

                new TodoItem(
                    "Also active")
            ]);

        var counts =
            service.GetCounts();

        Assert.Equal(
            3,
            counts.Total);

        Assert.Equal(
            2,
            counts.Active);

        Assert.Equal(
            1,
            counts.Completed);
    }

    [Fact]
    public void
        Default_lambda_parameter_and_explicit_override_format_labels()
    {
        TodoItem[] items =
        [
            new TodoItem(
                "Write tests"),

            new TodoItem(
                "Ship sample",
                isComplete:
                    true)
        ];

        string[] defaults =
            TodoFormatter.BuildLabels(
                items);

        string[] custom =
            TodoFormatter.BuildLabels(
                items,
                explicitPrefix:
                    "TASK");

        Assert.Equal(
            "TODO: Write tests [active]",
            defaults[0]);

        Assert.Equal(
            "TODO: Ship sample [done]",
            defaults[1]);

        Assert.Equal(
            "TASK: Write tests [active]",
            custom[0]);

        Assert.Equal(
            "TASK: Ship sample [done]",
            custom[1]);
    }

    [Fact]
    public void
        Active_filter_applies_limit_and_preserves_order()
    {
        var service =
            new TodoService(
            [
                new TodoItem(
                    "First active"),

                new TodoItem(
                    "Completed",
                    isComplete:
                        true),

                new TodoItem(
                    "Second active"),

                new TodoItem(
                    "Third active")
            ]);

        TodoItem[] active =
            service.GetActive(
                limit:
                    2);

        Assert.Collection(
            active,
            first =>
                Assert.Equal(
                    "First active",
                    first.Title),
            second =>
                Assert.Equal(
                    "Second active",
                    second.Title));

        Assert.Throws<
            ArgumentOutOfRangeException>(
                () =>
                    service.GetActive(
                        limit:
                            0));
    }
}