using System.ComponentModel.DataAnnotations;

namespace BlazorTodoMinimal;

public sealed class TodoState
{
    private readonly List<TodoItem>
        _items =
        [
            new TodoItem(
                1,
                "Learn component parameters",
                TodoPriority.Medium,
                false),

            new TodoItem(
                2,
                "Test the interactive form",
                TodoPriority.High,
                true)
        ];

    private int _nextId =
        2;

    public IReadOnlyList<TodoItem> Items =>
        _items
            .OrderBy(
                item =>
                    item.Id)
            .ToArray();

    public TodoItem Add(
        string title,
        TodoPriority priority)
    {
        int id =
            ++_nextId;

        var created =
            new TodoItem(
                id,
                title.Trim(),
                priority,
                false);

        _items.Add(
            created);

        return created;
    }

    public bool Toggle(
        int id)
    {
        int index =
            _items.FindIndex(
                item =>
                    item.Id == id);

        if (index < 0)
        {
            return false;
        }

        TodoItem current =
            _items[index];

        _items[index] =
            current with
            {
                IsCompleted =
                    !current.IsCompleted
            };

        return true;
    }

    public bool Delete(
        int id)
    {
        int index =
            _items.FindIndex(
                item =>
                    item.Id == id);

        if (index < 0)
        {
            return false;
        }

        _items.RemoveAt(
            index);

        return true;
    }
}

public sealed record TodoItem(
    int Id,
    string Title,
    TodoPriority Priority,
    bool IsCompleted);

public sealed class TodoInput
{
    [Required(
        ErrorMessage =
            "Title is required.")]
    [StringLength(
        80,
        MinimumLength = 3,
        ErrorMessage =
            "Title must contain between 3 and 80 characters.")]
    public string Title { get; set; } =
        string.Empty;

    public TodoPriority Priority
    {
        get;
        set;
    } = TodoPriority.Medium;
}

public enum TodoPriority
{
    Low,
    Medium,
    High
}

public enum TodoFilter
{
    All,
    Active,
    Completed
}