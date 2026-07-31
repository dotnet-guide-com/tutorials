using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TodoApiMinimal.Tests;

public sealed class TodoApiTests
{
    [Fact]
    public async Task
        Get_all_returns_seeded_todos()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current.CancellationToken;

        HttpResponseMessage response =
            await client.GetAsync(
                "/api/todos",
                ct);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        TodoItem[]? todos =
            await response.Content
                .ReadFromJsonAsync<TodoItem[]>(
                    ct);

        Assert.NotNull(todos);
        Assert.Equal(2, todos.Length);
        Assert.Equal(1, todos[0].Id);
        Assert.Equal(2, todos[1].Id);
    }

    [Fact]
    public async Task
        Get_missing_todo_returns_not_found()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current.CancellationToken;

        HttpResponseMessage response =
            await client.GetAsync(
                "/api/todos/999",
                ct);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task
        Create_returns_created_resource_and_location()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current.CancellationToken;

        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/todos",
                new CreateTodoRequest(
                    "Write integration tests"),
                ct);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        TodoItem? created =
            await response.Content
                .ReadFromJsonAsync<TodoItem>(
                    ct);

        Assert.NotNull(created);
        Assert.Equal(
            "Write integration tests",
            created.Title);

        Assert.Equal(
            $"/api/todos/{created.Id}",
            response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task
        Blank_title_returns_validation_problem()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current.CancellationToken;

        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/todos",
                new CreateTodoRequest(" "),
                ct);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task
        Update_changes_existing_todo()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current.CancellationToken;

        HttpResponseMessage updateResponse =
            await client.PutAsJsonAsync(
                "/api/todos/1",
                new UpdateTodoRequest(
                    "Learn route groups",
                    true),
                ct);

        Assert.Equal(
            HttpStatusCode.NoContent,
            updateResponse.StatusCode);

        TodoItem? updated =
            await client.GetFromJsonAsync<TodoItem>(
                "/api/todos/1",
                ct);

        Assert.NotNull(updated);
        Assert.Equal(
            "Learn route groups",
            updated.Title);
        Assert.True(updated.IsComplete);
    }

    [Fact]
    public async Task
        Delete_removes_existing_todo()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current.CancellationToken;

        HttpResponseMessage deleteResponse =
            await client.DeleteAsync(
                "/api/todos/1",
                ct);

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        HttpResponseMessage getResponse =
            await client.GetAsync(
                "/api/todos/1",
                ct);

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task
        Middleware_adds_request_id_header()
    {
        using var factory =
            new WebApplicationFactory<Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current.CancellationToken;

        HttpResponseMessage response =
            await client.GetAsync(
                "/health",
                ct);

        Assert.True(
            response.Headers.Contains(
                "X-Request-Id"));
    }
}