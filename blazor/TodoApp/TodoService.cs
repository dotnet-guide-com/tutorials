namespace TodoApp;

public class TodoService
{
    private List<TodoItem> _todos = new();

    public event Action? OnChange;

    public List<TodoItem> GetTodos() => _todos;

    public void AddTodo(TodoItem todo)
    {
        _todos.Add(todo);
        NotifyStateChanged();
    }

    public void RemoveTodo(TodoItem todo)
    {
        _todos.Remove(todo);
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

public class TodoItem
{
    public string Title { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
}
