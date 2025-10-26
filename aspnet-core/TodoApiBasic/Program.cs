using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// In-memory store (replace with a database in real apps)
var todos = new List<Todo>
{
    new(1, "Learn minimal APIs", false),
    new(2, "Build first endpoint", true)
};

// Routes
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/todos", () => Results.Ok(todos))
   .WithName("GetTodos")
   .WithOpenApi();

app.MapGet("/todos/{id:int}", (int id) =>
{
    var match = todos.FirstOrDefault(t => t.Id == id);
    return match is null ? Results.NotFound() : Results.Ok(match);
})
.WithName("GetTodoById")
.WithOpenApi();

app.MapPost("/todos", (Todo todo) =>
{
    // Simple validation example
    if (todo.Id <= 0 || string.IsNullOrWhiteSpace(todo.Title))
        return Results.BadRequest("Id and Title are required.");

    if (todos.Any(t => t.Id == todo.Id))
        return Results.Conflict($"Todo with id={todo.Id} already exists.");

    todos.Add(todo);
    return Results.Created($"/todos/{todo.Id}", todo);
})
.WithName("CreateTodo")
.WithOpenApi();

app.MapPut("/todos/{id:int}", (int id, Todo updated) =>
{
    var idx = todos.FindIndex(t => t.Id == id);
    if (idx == -1) return Results.NotFound();

    todos[idx] = updated with { Id = id };
    return Results.NoContent();
})
.WithName("UpdateTodo")
.WithOpenApi();

app.MapDelete("/todos/{id:int}", (int id) =>
{
    var removed = todos.RemoveAll(t => t.Id == id) > 0;
    return removed ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteTodo")
.WithOpenApi();

app.Run();
