using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () =>
            HealthCheckResult.Healthy(
                "Process is running."),
        tags:
        [
            "live"
        ])
    .AddCheck<
        ConfigurationHealthCheck>(
        "configuration",
        tags:
        [
            "ready"
        ]);

var app =
    builder.Build();

TodoItem[] todos =
[
    new TodoItem(
        1,
        "Build the multi-stage image",
        false),

    new TodoItem(
        2,
        "Run the container as non-root",
        false),

    new TodoItem(
        3,
        "Verify the health probe",
        true)
];

app.MapGet(
    "/",
    () =>
        TypedResults.Ok(
            new ServiceDescription(
                "ContainerizedApiMinimal",
                [
                    "GET /api/todos",
                    "GET /api/todos/{id}",
                    "GET /info",
                    "GET /health/live",
                    "GET /health/ready",
                    "GET /health"
                ])));

app.MapGet(
    "/api/todos",
    () =>
        TypedResults.Ok(
            todos));

app.MapGet(
    "/api/todos/{id:int:min(1)}",
    (int id) =>
    {
        TodoItem? todo =
            todos.FirstOrDefault(
                item =>
                    item.Id == id);

        return todo is null
            ? Results.NotFound()
            : Results.Ok(todo);
    });

app.MapGet(
    "/info",
    (
        IConfiguration configuration,
        IHostEnvironment environment) =>
    {
        bool runningInContainer =
            bool.TryParse(
                Environment.GetEnvironmentVariable(
                    "DOTNET_RUNNING_IN_CONTAINER"),
                out bool parsed)
            && parsed;

        return TypedResults.Ok(
            new RuntimeInfo(
                "ContainerizedApiMinimal",
                environment.EnvironmentName,
                configuration[
                    "Sample:Message"]
                    ?? string.Empty,
                runningInContainer,
                Environment.UserName));
    });

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate =
            registration =>
                registration.Tags.Contains(
                    "live")
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate =
            registration =>
                registration.Tags.Contains(
                    "ready")
    });

app.MapHealthChecks(
    "/health");

app.Run();

public sealed record TodoItem(
    int Id,
    string Title,
    bool IsComplete);

public sealed record ServiceDescription(
    string Name,
    string[] Endpoints);

public sealed record RuntimeInfo(
    string Application,
    string Environment,
    string Message,
    bool RunningInContainer,
    string User);

public sealed class
    ConfigurationHealthCheck(
        IConfiguration configuration) :
        IHealthCheck
{
    public Task<HealthCheckResult>
        CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken =
                default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        string? message =
            configuration[
                "Sample:Message"];

        HealthCheckResult result =
            string.IsNullOrWhiteSpace(
                message)
                ? HealthCheckResult.Unhealthy(
                    "Sample:Message is not configured.")
                : HealthCheckResult.Healthy(
                    "Required runtime configuration is present.");

        return Task.FromResult(
            result);
    }
}

public partial class Program
{
}