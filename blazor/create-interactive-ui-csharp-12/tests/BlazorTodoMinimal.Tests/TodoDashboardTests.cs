using BlazorTodoMinimal.Components.Pages;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorTodoMinimal.Tests;

public sealed class TodoDashboardTests
{
    [Fact]
    public void
        Dashboard_renders_seeded_items_and_summary()
    {
        using BunitContext context =
            CreateContext();

        var cut =
            context.Render<Todos>();

        Assert.Equal(
            2,
            cut.FindAll(
                "[data-testid='todo-item']")
                .Count);

        string summary =
            cut.Find(
                    "[data-testid='summary']")
                .TextContent;

        Assert.Contains(
            "2 total",
            summary,
            StringComparison.Ordinal);

        Assert.Contains(
            "1 completed",
            summary,
            StringComparison.Ordinal);
    }

    [Fact]
    public void
        Valid_form_submission_adds_a_todo()
    {
        using BunitContext context =
            CreateContext();

        var cut =
            context.Render<Todos>();

        cut.Find(
                "#todo-title")
            .Change(
                "Write bUnit tests");

        cut.Find(
                "#todo-priority")
            .Change(
                TodoPriority.High
                    .ToString());

        cut.Find(
                "form")
            .Submit();

        cut.WaitForAssertion(
            () =>
            {
                Assert.Equal(
                    3,
                    cut.FindAll(
                        "[data-testid='todo-item']")
                        .Count);

                Assert.Contains(
                    "Write bUnit tests",
                    cut.Markup,
                    StringComparison.Ordinal);
            });
    }

    [Fact]
    public void
        Invalid_title_shows_validation_and_does_not_add()
    {
        using BunitContext context =
            CreateContext();

        var cut =
            context.Render<Todos>();

        cut.Find(
                "#todo-title")
            .Change(
                "x");

        cut.Find(
                "form")
            .Submit();

        cut.WaitForAssertion(
            () =>
            {
                Assert.Contains(
                    "Title must contain between 3 and 80 characters.",
                    cut.Markup,
                    StringComparison.Ordinal);

                Assert.Equal(
                    2,
                    cut.FindAll(
                        "[data-testid='todo-item']")
                        .Count);
            });
    }

    [Fact]
    public void
        Toggle_callback_updates_item_and_summary()
    {
        using BunitContext context =
            CreateContext();

        var cut =
            context.Render<Todos>();

        cut.Find(
                "[data-todo-id='1'] [data-action='toggle']")
            .Click();

        cut.WaitForAssertion(
            () =>
            {
                string summary =
                    cut.Find(
                            "[data-testid='summary']")
                        .TextContent;

                Assert.Contains(
                    "2 completed",
                    summary,
                    StringComparison.Ordinal);

                Assert.Contains(
                    "Mark active",
                    cut.Find(
                            "[data-todo-id='1'] [data-action='toggle']")
                        .TextContent,
                    StringComparison.Ordinal);
            });
    }

    [Fact]
    public void
        Completed_filter_shows_only_completed_items()
    {
        using BunitContext context =
            CreateContext();

        var cut =
            context.Render<Todos>();

        cut.Find(
                "#todo-filter")
            .Change(
                TodoFilter.Completed
                    .ToString());

        cut.WaitForAssertion(
            () =>
            {
                IReadOnlyList<
                    AngleSharp.Dom.IElement> items =
                        cut.FindAll(
                            "[data-testid='todo-item']");

                Assert.Single(
                    items);

                Assert.Contains(
                    "Test the interactive form",
                    items[0].TextContent,
                    StringComparison.Ordinal);
            });
    }

    [Fact]
    public void
        Delete_callback_removes_the_selected_item()
    {
        using BunitContext context =
            CreateContext();

        var cut =
            context.Render<Todos>();

        cut.Find(
                "[data-todo-id='2'] [data-action='delete']")
            .Click();

        cut.WaitForAssertion(
            () =>
            {
                Assert.Single(
                    cut.FindAll(
                        "[data-testid='todo-item']"));

                Assert.DoesNotContain(
                    "Test the interactive form",
                    cut.Markup,
                    StringComparison.Ordinal);
            });
    }

    [Fact]
    public void
        State_add_trims_title_and_allocates_next_id()
    {
        var state =
            new TodoState();

        TodoItem created =
            state.Add(
                "  Review render modes  ",
                TodoPriority.Low);

        Assert.Equal(
            3,
            created.Id);

        Assert.Equal(
            "Review render modes",
            created.Title);

        Assert.False(
            created.IsCompleted);
    }

    private static BunitContext
        CreateContext()
    {
        var context =
            new BunitContext();

        context.Services.AddScoped<
            TodoState>();

        return context;
    }
}