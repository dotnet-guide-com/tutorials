using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Channels;
using BackgroundJobQueueMinimal.Jobs;
using BackgroundJobQueueMinimal.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BackgroundJobQueueMinimal.Tests;

public sealed class BackgroundJobQueueTests
{
    [Fact]
    public void
        Queue_rejects_nonpositive_capacity()
    {
        Assert.Throws<
            ArgumentOutOfRangeException>(
                () =>
                    CreateQueue(
                        0));

        Assert.Throws<
            ArgumentOutOfRangeException>(
                () =>
                    CreateQueue(
                        -1));
    }

    [Fact]
    public async Task
        Queue_preserves_fifo_and_reports_depth()
    {
        BoundedBackgroundJobQueue queue =
            CreateQueue(
                3);

        EmailJob first =
            CreateJob(
                "first");

        EmailJob second =
            CreateJob(
                "second");

        await queue.EnqueueAsync(
            first,
            TestContext.Current
                .CancellationToken);

        await queue.EnqueueAsync(
            second,
            TestContext.Current
                .CancellationToken);

        Assert.Equal(
            2,
            queue.Depth);

        await using
            IAsyncEnumerator<
                EmailJob>
                reader =
                    queue
                        .ReadAllAsync(
                            TestContext
                                .Current
                                .CancellationToken)
                        .GetAsyncEnumerator(
                            TestContext
                                .Current
                                .CancellationToken);

        Assert.True(
            await reader
                .MoveNextAsync());

        Assert.Equal(
            first.JobId,
            reader.Current.JobId);

        Assert.True(
            await reader
                .MoveNextAsync());

        Assert.Equal(
            second.JobId,
            reader.Current.JobId);

        Assert.Equal(
            0,
            queue.Depth);
    }

    [Fact]
    public async Task
        Bounded_queue_waits_until_capacity_is_available()
    {
        BoundedBackgroundJobQueue queue =
            CreateQueue(
                1);

        EmailJob first =
            CreateJob(
                "first");

        EmailJob second =
            CreateJob(
                "second");

        await queue.EnqueueAsync(
            first,
            TestContext.Current
                .CancellationToken);

        Task secondWrite =
            queue.EnqueueAsync(
                    second,
                    TestContext.Current
                        .CancellationToken)
                .AsTask();

        Assert.False(
            secondWrite.IsCompleted);

        await using
            IAsyncEnumerator<
                EmailJob>
                reader =
                    queue
                        .ReadAllAsync(
                            TestContext
                                .Current
                                .CancellationToken)
                        .GetAsyncEnumerator(
                            TestContext
                                .Current
                                .CancellationToken);

        Assert.True(
            await reader
                .MoveNextAsync());

        Assert.Equal(
            first.JobId,
            reader.Current.JobId);

        await secondWrite
            .WaitAsync(
                TestContext.Current
                    .CancellationToken);

        Assert.True(
            await reader
                .MoveNextAsync());

        Assert.Equal(
            second.JobId,
            reader.Current.JobId);

        // Second scenario: cancellation of a pending bounded write
        {
            BoundedBackgroundJobQueue cancelQueue =
                CreateQueue(
                    1);

            EmailJob blocking =
                CreateJob(
                    "blocking");

            EmailJob canceled =
                CreateJob(
                    "canceled");

            await cancelQueue.EnqueueAsync(
                blocking,
                TestContext.Current
                    .CancellationToken);

            using var cancelSource =
                new CancellationTokenSource();

            Task canceledWrite =
                cancelQueue.EnqueueAsync(
                        canceled,
                        cancelSource.Token)
                    .AsTask();

            Assert.False(
                canceledWrite.IsCompleted);

            cancelSource.Cancel();

            await Assert.ThrowsAsync<
                OperationCanceledException>(
                    async () =>
                        await canceledWrite);

            Assert.Equal(
                1,
                cancelQueue.Depth);

            await using
                IAsyncEnumerator<
                    EmailJob>
                    cancelReader =
                        cancelQueue
                            .ReadAllAsync(
                                TestContext
                                    .Current
                                    .CancellationToken)
                            .GetAsyncEnumerator(
                                TestContext
                                    .Current
                                    .CancellationToken);

            Assert.True(
                await cancelReader
                    .MoveNextAsync());

            Assert.Equal(
                blocking.JobId,
                cancelReader.Current.JobId);

            cancelQueue.Complete();
        }
    }

    [Fact]
    public async Task
        Completion_drains_buffer_and_rejects_new_writes()
    {
        BoundedBackgroundJobQueue queue =
            CreateQueue(
                2);

        EmailJob first =
            CreateJob(
                "first");

        EmailJob second =
            CreateJob(
                "second");

        await queue.EnqueueAsync(
            first,
            TestContext.Current
                .CancellationToken);

        await queue.EnqueueAsync(
            second,
            TestContext.Current
                .CancellationToken);

        queue.Complete();
        queue.Complete();

        Assert.False(
            queue.IsAccepting);

        var received =
            new List<Guid>();

        await foreach (
            EmailJob job
            in queue.ReadAllAsync(
                TestContext.Current
                    .CancellationToken))
        {
            received.Add(
                job.JobId);
        }

        Assert.Equal(
            [
                first.JobId,
                second.JobId
            ],
            received);

        await Assert.ThrowsAsync<
            ChannelClosedException>(
                async () =>
                    await queue.EnqueueAsync(
                        CreateJob(
                            "late"),
                        TestContext
                            .Current
                            .CancellationToken));
    }

    [Fact]
    public async Task
        Worker_uses_fresh_scopes_and_isolates_job_failure()
    {
        BoundedBackgroundJobQueue queue =
            CreateQueue(
                4);

        var tracker =
            new InMemoryJobTracker();

        var recorder =
            new HandlerRecorder();

        var services =
            new ServiceCollection();

        services.AddSingleton(
            recorder);

        services.AddScoped<
            ScopedMarker>();

        services.AddScoped<
            IEmailJobHandler,
            RecordingEmailJobHandler>();

        await using
            ServiceProvider provider =
                services
                    .BuildServiceProvider();

        var worker =
            new QueuedEmailWorker(
                queue,
                tracker,
                provider
                    .GetRequiredService<
                        IServiceScopeFactory>(),
                NullLogger<
                    QueuedEmailWorker>
                    .Instance);

        EmailJob failing =
            CreateJob(
                "fail");

        EmailJob succeeding =
            CreateJob(
                "succeed");

        tracker.Register(
            failing);

        tracker.Register(
            succeeding);

        await worker.StartAsync(
            TestContext.Current
                .CancellationToken);

        await queue.EnqueueAsync(
            failing,
            TestContext.Current
                .CancellationToken);

        await queue.EnqueueAsync(
            succeeding,
            TestContext.Current
                .CancellationToken);

        JobSnapshot first =
            await tracker
                .WaitForTerminalAsync(
                    failing.JobId,
                    TestContext
                        .Current
                        .CancellationToken);

        JobSnapshot second =
            await tracker
                .WaitForTerminalAsync(
                    succeeding.JobId,
                    TestContext
                        .Current
                        .CancellationToken);

        Assert.Equal(
            JobState.Failed,
            first.State);

        Assert.Equal(
            "JOB_PROCESSING_FAILED",
            first.FailureCode);

        Assert.Equal(
            "Job processing failed.",
            first.FailureMessage);

        Assert.Equal(
            JobState.Succeeded,
            second.State);

        Assert.Equal(
            2,
            recorder.ScopeIds
                .Distinct()
                .Count());

        // Verify TryGet returns the same terminal state
        Assert.True(
            tracker.TryGet(
                failing.JobId,
                out JobSnapshot?
                    tryGetFirst));

        Assert.NotNull(
            tryGetFirst);

        Assert.Equal(
            first.State,
            tryGetFirst.State);

        Assert.Equal(
            first.FailureCode,
            tryGetFirst.FailureCode);

        Assert.Equal(
            first.FailureMessage,
            tryGetFirst.FailureMessage);

        Assert.True(
            tracker.TryGet(
                succeeding.JobId,
                out JobSnapshot?
                    tryGetSecond));

        Assert.NotNull(
            tryGetSecond);

        Assert.Equal(
            second.State,
            tryGetSecond.State);

        Assert.Null(
            tryGetSecond.FailureCode);

        Assert.Null(
            tryGetSecond.FailureMessage);

        using var stop =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(
                    2));

        await worker.StopAsync(
            stop.Token);
    }

    [Fact]
    public async Task
        Worker_cancellation_marks_inflight_job_canceled()
    {
        BoundedBackgroundJobQueue queue =
            CreateQueue(
                1);

        var tracker =
            new InMemoryJobTracker();

        var services =
            new ServiceCollection();

        services.AddScoped<
            IEmailJobHandler,
            BlockingEmailJobHandler>();

        await using
            ServiceProvider provider =
                services
                    .BuildServiceProvider();

        var worker =
            new QueuedEmailWorker(
                queue,
                tracker,
                provider
                    .GetRequiredService<
                        IServiceScopeFactory>(),
                NullLogger<
                    QueuedEmailWorker>
                    .Instance);

        EmailJob job =
            CreateJob(
                "block");

        tracker.Register(
            job);

        await worker.StartAsync(
            TestContext.Current
                .CancellationToken);

        await queue.EnqueueAsync(
            job,
            TestContext.Current
                .CancellationToken);

        await WaitForStateAsync(
            tracker,
            job.JobId,
            JobState.Running,
            TestContext.Current
                .CancellationToken);

        using var stop =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(
                    2));

        await worker.StopAsync(
            stop.Token);

        JobSnapshot snapshot =
            await tracker
                .WaitForTerminalAsync(
                    job.JobId,
                    TestContext
                        .Current
                        .CancellationToken);

        Assert.Equal(
            JobState.Canceled,
            snapshot.State);

        Assert.False(
            queue.IsAccepting);
    }

    [Fact]
    public async Task
        Api_accepts_job_and_reports_terminal_status()
    {
        await using var factory =
            new WebApplicationFactory<
                Program>();

        HttpClient client =
            factory.CreateClient();

        HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/jobs/email",
                new EmailJobRequest(
                    "reader@example.com",
                    "Queue sample"),
                TestContext.Current
                    .CancellationToken);

        Assert.Equal(
            HttpStatusCode.Accepted,
            response.StatusCode);

        Assert.NotNull(
            response.Headers.Location);

        AcceptedJob? accepted =
            await response.Content
                .ReadFromJsonAsync<
                    AcceptedJob>(
                        TestContext.Current
                            .CancellationToken);

        Assert.NotNull(
            accepted);

        Assert.NotEqual(
            Guid.Empty,
            accepted.JobId);

        JobSnapshot snapshot =
            await PollTerminalStatusAsync(
                client,
                accepted.JobId,
                TestContext.Current
                    .CancellationToken);

        Assert.Equal(
            JobState.Succeeded,
            snapshot.State);

        Assert.Null(
            snapshot.FailureMessage);
    }

    [Fact]
    public async Task
        Api_rejects_invalid_request_and_unknown_job()
    {
        await using var factory =
            new WebApplicationFactory<
                Program>();

        HttpClient client =
            factory.CreateClient();

        HttpResponseMessage invalid =
            await client.PostAsJsonAsync(
                "/jobs/email",
                new EmailJobRequest(
                    "",
                    ""),
                TestContext.Current
                    .CancellationToken);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            invalid.StatusCode);

        HttpResponseMessage missing =
            await client.GetAsync(
                $"/jobs/{Guid.NewGuid()}",
                TestContext.Current
                    .CancellationToken);

        Assert.Equal(
            HttpStatusCode.NotFound,
            missing.StatusCode);

        QueueSnapshot? queue =
            await client
                .GetFromJsonAsync<
                    QueueSnapshot>(
                        "/jobs/queue",
                        TestContext.Current
                            .CancellationToken);

        Assert.NotNull(
            queue);

        Assert.True(
            queue.Accepting);

        Assert.Equal(
            0,
            queue.Depth);
    }

    private static
        BoundedBackgroundJobQueue
        CreateQueue(
            int capacity) =>
                new(
                    Options.Create(
                        new
                            BackgroundJobQueueOptions
                        {
                            Capacity =
                                capacity
                        }));

    private static EmailJob CreateJob(
        string subject) =>
            new(
                JobId:
                    Guid.NewGuid(),

                To:
                    "reader@example.com",

                Subject:
                    subject,

                EnqueuedAt:
                    DateTimeOffset
                        .UnixEpoch);

    private static async Task
        WaitForStateAsync(
            IJobTracker tracker,
            Guid jobId,
            JobState expected,
            CancellationToken
                cancellationToken)
    {
        while (true)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            if (tracker.TryGet(
                    jobId,
                    out JobSnapshot?
                        snapshot)
                && snapshot!
                    .State
                    == expected)
            {
                return;
            }

            await Task.Delay(
                10,
                cancellationToken);
        }
    }

    private static async Task<
        JobSnapshot>
        PollTerminalStatusAsync(
            HttpClient client,
            Guid jobId,
            CancellationToken
                cancellationToken)
    {
        while (true)
        {
            JobSnapshot? snapshot =
                await client
                    .GetFromJsonAsync<
                        JobSnapshot>(
                            $"/jobs/{jobId}",
                            cancellationToken);

            Assert.NotNull(
                snapshot);

            if (snapshot.State
                is JobState.Succeeded
                or JobState.Failed
                or JobState.Canceled)
            {
                return snapshot;
            }

            await Task.Delay(
                10,
                cancellationToken);
        }
    }

    private sealed record AcceptedJob(
        Guid JobId,
        string State);

    private sealed class HandlerRecorder
    {
        public ConcurrentBag<Guid>
            ScopeIds
        {
            get;
        } = new();
    }

    private sealed class ScopedMarker
    {
        public Guid Id
        {
            get;
        } =
            Guid.NewGuid();
    }

    private sealed class
        RecordingEmailJobHandler(
            ScopedMarker marker,
            HandlerRecorder recorder) :
        IEmailJobHandler
    {
        public Task HandleAsync(
            EmailJob job,
            CancellationToken
                cancellationToken)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            recorder.ScopeIds.Add(
                marker.Id);

            if (job.Subject
                == "fail")
            {
                throw new
                    InvalidOperationException(
                        "Deterministic test failure.");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class
        BlockingEmailJobHandler :
        IEmailJobHandler
    {
        public Task HandleAsync(
            EmailJob job,
            CancellationToken
                cancellationToken) =>
                    Task.Delay(
                        Timeout.InfiniteTimeSpan,
                        cancellationToken);
    }
}