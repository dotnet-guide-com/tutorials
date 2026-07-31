using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<
    ITodoRepository,
    InMemoryTodoRepository>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Request-Id"] =
        context.TraceIdentifier;

    await next();
});

app.MapGet("/", () => Results.Ok(new
{
    name = "Todo API Minimal",
    endpoints = new[]
    {
        "GET /health",
        "GET /api/todos",
        "GET /api/todos/{id}",
        "POST /api/todos",
        "PUT /api/todos/{id}",
        "DELETE /api/todos/{id}"
    }
}));

app.MapGet("/health", () =>
    Results.Ok(new
    {
        status = "ok"
    }));

RouteGroupBuilder todos =
    app.MapGroup("/api/todos");

todos.MapGet(
    "/",
    (ITodoRepository repository) =>
        Results.Ok(repository.GetAll()));

todos.MapGet(
    "/{id:int:min(1)}",
    (int id, ITodoRepository repository) =>
    {
        TodoItem? todo =
            repository.GetById(id);

        return todo is null
            ? Results.NotFound()
            : Results.Ok(todo);
    });

todos.MapPost(
    "/",
    (
        CreateTodoRequest request,
        ITodoRepository repository) =>
    {
        Dictionary<string, string[]>? errors =
            ValidateTitle(request.Title);

        if (errors is not null)
        {
            return Results.ValidationProblem(errors);
        }

        TodoItem created =
            repository.Create(
                request.Title!.Trim());

        return Results.Created(
            $"/api/todos/{created.Id}",
            created);
    });

todos.MapPut(
    "/{id:int:min(1)}",
    (
        int id,
        UpdateTodoRequest request,
        ITodoRepository repository) =>
    {
        Dictionary<string, string[]>? errors =
            ValidateTitle(request.Title);

        if (errors is not null)
        {
            return Results.ValidationProblem(errors);
        }

        TodoItem? updated =
            repository.Update(
                id,
                request.Title!.Trim(),
                request.IsComplete);

        return updated is null
            ? Results.NotFound()
            : Results.NoContent();
    });

todos.MapDelete(
    "/{id:int:min(1)}",
    (int id, ITodoRepository repository) =>
        repository.Delete(id)
            ? Results.NoContent()
            : Results.NotFound());

app.Run();

static Dictionary<string, string[]>?
    ValidateTitle(string? title)
{
    if (string.IsNullOrWhiteSpace(title))
    {
        return new Dictionary<string, string[]>
        {
            ["title"] =
            [
                "Title is required."
            ]
        };
    }

    if (title.Trim().Length > 100)
    {
        return new Dictionary<string, string[]>
        {
            ["title"] =
            [
                "Title must contain 100 characters or fewer."
            ]
        };
    }

    return null;
}

public sealed record TodoItem(
    int Id,
    string Title,
    bool IsComplete);

public sealed record CreateTodoRequest(
    string? Title);

public sealed record UpdateTodoRequest(
    string? Title,
    bool IsComplete);

public interface ITodoRepository
{
    IReadOnlyList<TodoItem> GetAll();

    TodoItem? GetById(int id);

    TodoItem Create(string title);

    TodoItem? Update(
        int id,
        string title,
        bool isComplete);

    bool Delete(int id);
}

public sealed class InMemoryTodoRepository :
    ITodoRepository
{
    private readonly
        ConcurrentDictionary<int, TodoItem> _items =
            new();

    private int _nextId;

    public InMemoryTodoRepository()
    {
        _items[1] =
            new TodoItem(
                1,
                "Learn ASP.NET Core",
                false);

        _items[2] =
            new TodoItem(
                2,
                "Build a Todo API",
                false);

        _nextId = 2;
    }

    public IReadOnlyList<TodoItem> GetAll() =>
        _items.Values
            .OrderBy(item => item.Id)
            .ToArray();

    public TodoItem? GetById(int id) =>
        _items.TryGetValue(
            id,
            out TodoItem? item)
                ? item
                : null;

    public TodoItem Create(string title)
    {
        int id =
            Interlocked.Increment(
                ref _nextId);

        var item =
            new TodoItem(
                id,
                title,
                false);

        _items[id] = item;

        return item;
    }

    public TodoItem? Update(
        int id,
        string title,
        bool isComplete)
    {
        while (_items.TryGetValue(
                   id,
                   out TodoItem? current))
        {
            TodoItem updated =
                current with
                {
                    Title = title,
                    IsComplete = isComplete
                };

            if (_items.TryUpdate(
                    id,
                    updated,
                    current))
            {
                return updated;
            }
        }

        return null;
    }

    public bool Delete(int id) =>
        _items.TryRemove(
            id,
            out _);
}

public partial class Program
{
}