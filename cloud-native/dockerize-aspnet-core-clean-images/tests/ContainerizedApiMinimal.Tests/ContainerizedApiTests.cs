using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ContainerizedApiMinimal.Tests;

public sealed class ContainerizedApiTests
{
    [Fact]
    public async Task
        Root_describes_the_sample_endpoints()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        ServiceDescription? description =
            await client.GetFromJsonAsync<
                ServiceDescription>(
                "/",
                ct);

        Assert.NotNull(
            description);

        Assert.Equal(
            "ContainerizedApiMinimal",
            description.Name);

        Assert.Contains(
            "GET /health/ready",
            description.Endpoints);
    }

    [Fact]
    public async Task
        Todos_returns_the_seeded_items()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        TodoItem[]? todos =
            await client.GetFromJsonAsync<
                TodoItem[]>(
                "/api/todos",
                ct);

        Assert.NotNull(
            todos);

        Assert.Equal(
            3,
            todos.Length);

        Assert.Contains(
            todos,
            todo =>
                todo.Title ==
                "Run the container as non-root");
    }

    [Fact]
    public async Task
        Missing_todo_returns_not_found()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

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
        Live_and_ready_health_endpoints_are_healthy()
    {
        using var factory =
            new WebApplicationFactory<
                Program>();

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        // -- Normal configuration: all endpoints healthy
        HttpResponseMessage live =
            await client.GetAsync(
                "/health/live",
                ct);

        HttpResponseMessage ready =
            await client.GetAsync(
                "/health/ready",
                ct);

        HttpResponseMessage combined =
            await client.GetAsync(
                "/health",
                ct);

        Assert.Equal(
            HttpStatusCode.OK,
            live.StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            ready.StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            combined.StatusCode);

        // -- Blank configuration: liveness stays 200, readiness degrades to 503
        using WebApplicationFactory<
            Program> blankFactory =
                factory.WithWebHostBuilder(
                    builder =>
                        builder.ConfigureAppConfiguration(
                            (
                                context,
                                configuration) =>
                            {
                                configuration
                                    .AddInMemoryCollection(
                                        new Dictionary<
                                            string,
                                            string?>
                                        {
                                            ["Sample:Message"] =
                                                string.Empty
                                        });
                            }));

        using HttpClient blankClient =
            blankFactory.CreateClient();

        CancellationToken blankCt =
            TestContext.Current
                .CancellationToken;

        HttpResponseMessage blankLive =
            await blankClient.GetAsync(
                "/health/live",
                blankCt);

        HttpResponseMessage blankReady =
            await blankClient.GetAsync(
                "/health/ready",
                blankCt);

        HttpResponseMessage blankCombined =
            await blankClient.GetAsync(
                "/health",
                blankCt);

        Assert.Equal(
            HttpStatusCode.OK,
            blankLive.StatusCode);

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            blankReady.StatusCode);

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            blankCombined.StatusCode);
    }

    [Fact]
    public async Task
        Runtime_configuration_can_override_appsettings()
    {
        using var baseFactory =
            new WebApplicationFactory<
                Program>();

        using WebApplicationFactory<
            Program> factory =
                baseFactory.WithWebHostBuilder(
                    builder =>
                        builder.ConfigureAppConfiguration(
                            (
                                context,
                                configuration) =>
                            {
                                configuration
                                    .AddInMemoryCollection(
                                        new Dictionary<
                                            string,
                                            string?>
                                        {
                                            ["Sample:Message"] =
                                                "Overridden by integration test"
                                        });
                            }));

        using HttpClient client =
            factory.CreateClient();

        CancellationToken ct =
            TestContext.Current
                .CancellationToken;

        RuntimeInfo? info =
            await client.GetFromJsonAsync<
                RuntimeInfo>(
                "/info",
                ct);

        Assert.NotNull(
            info);

        Assert.Equal(
            "Overridden by integration test",
            info.Message);
    }
}