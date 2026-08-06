using System.Threading.Channels;
using BackgroundJobQueueMinimal.Jobs;
using BackgroundJobQueueMinimal.Services;

WebApplicationBuilder builder =
    WebApplication.CreateBuilder(
        args);

builder.Services
    .Configure<
        BackgroundJobQueueOptions>(
            builder.Configuration
                .GetSection(
                    BackgroundJobQueueOptions
                        .SectionName));

builder.Services
    .AddSingleton<
        IBackgroundJobQueue,
        BoundedBackgroundJobQueue>();

builder.Services
    .AddSingleton<
        IJobTracker,
        InMemoryJobTracker>();

builder.Services
    .AddScoped<
        IEmailJobHandler,
        FakeEmailJobHandler>();

builder.Services
    .AddHostedService<
        QueuedEmailWorker>();

builder.Services
    .AddSingleton(
        TimeProvider.System);

WebApplication app =
    builder.Build();

app.MapGet(
    "/",
    (
        IBackgroundJobQueue
            queue) =>
        Results.Ok(
            new
            {
                Sample =
                    "bounded-background-job-queue",

                Durability =
                    "in-memory",

                queue.Capacity
            }));

app.MapPost(
    "/jobs/email",
    async (
        EmailJobRequest request,
        IBackgroundJobQueue queue,
        IJobTracker tracker,
        TimeProvider timeProvider,
        CancellationToken
            cancellationToken) =>
    {
        Dictionary<string, string[]>
            errors =
                Validate(
                    request);

        if (errors.Count
            > 0)
        {
            return Results
                .ValidationProblem(
                    errors);
        }

        var job =
            new EmailJob(
                JobId:
                    Guid.NewGuid(),

                To:
                    request.To!.Trim(),

                Subject:
                    request.Subject!.Trim(),

                EnqueuedAt:
                    timeProvider
                        .GetUtcNow());

        tracker.Register(
            job);

        try
        {
            await queue.EnqueueAsync(
                job,
                cancellationToken);
        }
        catch (
            OperationCanceledException)
            when (
                cancellationToken
                    .IsCancellationRequested)
        {
            tracker.Remove(
                job.JobId);

            throw;
        }
        catch (
            ChannelClosedException)
        {
            tracker.Remove(
                job.JobId);

            return Results.Problem(
                statusCode:
                    StatusCodes
                        .Status503ServiceUnavailable,

                title:
                    "Queue admission is closed.",

                detail:
                    "The application is stopping and is not accepting new jobs.");
        }

        return Results.Accepted(
            $"/jobs/{job.JobId}",
            new
            {
                job.JobId,
                State =
                    JobState.Queued
                        .ToString()
            });
    });

app.MapGet(
    "/jobs/{jobId:guid}",
    (
        Guid jobId,
        IJobTracker tracker) =>
            tracker.TryGet(
                jobId,
                out JobSnapshot?
                    snapshot)
                ? Results.Ok(
                    snapshot)
                : Results.NotFound());

app.MapGet(
    "/jobs/queue",
    (
        IBackgroundJobQueue
            queue) =>
        Results.Ok(
            new QueueSnapshot(
                Capacity:
                    queue.Capacity,

                Depth:
                    queue.Depth,

                Accepting:
                    queue.IsAccepting)));

app.Run();

static Dictionary<
    string,
    string[]>
    Validate(
        EmailJobRequest request)
{
    var errors =
        new Dictionary<
            string,
            string[]>(
                StringComparer
                    .Ordinal);

    if (string.IsNullOrWhiteSpace(
            request.To)
        || !request.To.Contains(
            '@',
            StringComparison.Ordinal))
    {
        errors["to"] =
        [
            "A recipient email address is required."
        ];
    }

    if (string.IsNullOrWhiteSpace(
            request.Subject))
    {
        errors["subject"] =
        [
            "A subject is required."
        ];
    }
    else if (request.Subject.Length
        > 100)
    {
        errors["subject"] =
        [
            "The subject must not exceed 100 characters."
        ];
    }

    return errors;
}

public partial class Program;