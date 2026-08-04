using CSharp12RefactoringLab.Models;

using TodoCounts =
    (
        int Total,
        int Active,
        int Completed
    );

namespace CSharp12RefactoringLab.Services;

public sealed class TodoService(
    IEnumerable<TodoItem> seedItems)
{
    private readonly List<TodoItem>
        _items =
        [
            .. seedItems
        ];

    public void Add(
        TodoItem item)
    {
        ArgumentNullException
            .ThrowIfNull(
                item);

        _items.Add(
            item);
    }

    public TodoItem[] Snapshot() =>
        [
            .. _items
        ];

    public TodoItem[] GetActive(
        int limit = 10)
    {
        ArgumentOutOfRangeException
            .ThrowIfNegativeOrZero(
                limit);

        return
        [
            .. _items
                .Where(
                    item =>
                        !item.IsComplete)
                .Take(
                    limit)
        ];
    }

    public TodoItem[] GetAllWithWelcome()
    {
        TodoItem welcome =
            new(
                "Welcome to the C# 12 lab",
                isComplete:
                    true);

        return
        [
            welcome,
            .. _items
        ];
    }

    public TodoCounts GetCounts()
    {
        int completed =
            _items.Count(
                item =>
                    item.IsComplete);

        return (
            Total:
                _items.Count,

            Active:
                _items.Count
                - completed,

            Completed:
                completed);
    }
}