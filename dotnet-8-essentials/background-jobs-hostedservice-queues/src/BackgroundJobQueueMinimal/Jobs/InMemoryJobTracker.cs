using System.Collections.Concurrent;

namespace BackgroundJobQueueMinimal.Jobs;

public sealed class InMemoryJobTracker :
    IJobTracker
{
    private readonly
        ConcurrentDictionary<
            Guid,
            Entry>
        _entries =
            new();

    public void Register(
        EmailJob job)
    {
        ArgumentNullException
            .ThrowIfNull(
                job);

        var snapshot =
            new JobSnapshot(
                JobId:
                    job.JobId,

                JobName:
                    "send-email",

                State:
                    JobState.Queued,

                EnqueuedAt:
                    job.EnqueuedAt);

        if (!_entries.TryAdd(
                job.JobId,
                new Entry(
                    snapshot)))
        {
            throw new
                InvalidOperationException(
                    $"Job '{job.JobId}' is already registered.");
        }
    }

    public void MarkRunning(
        Guid jobId) =>
            Update(
                jobId,
                current =>
                    current
                    with
                    {
                        State =
                            JobState.Running
                    });

    public void MarkSucceeded(
        Guid jobId) =>
            Complete(
                jobId,
                current =>
                    current
                    with
                    {
                        State =
                            JobState.Succeeded
                    });

    public void MarkFailed(
        Guid jobId,
        string failureCode,
        string failureMessage)
    {
        ArgumentException
            .ThrowIfNullOrWhiteSpace(
                failureCode);

        ArgumentException
            .ThrowIfNullOrWhiteSpace(
                failureMessage);

        Complete(
            jobId,
            current =>
                current
                with
                {
                    State =
                        JobState.Failed,

                    FailureCode =
                        failureCode,

                    FailureMessage =
                        failureMessage
                });
    }

    public void MarkCanceled(
        Guid jobId) =>
            Complete(
                jobId,
                current =>
                    current
                    with
                    {
                        State =
                            JobState.Canceled,

                        FailureCode =
                            "JOB_CANCELED",

                        FailureMessage =
                            "Job processing was canceled."
                    });

    public bool TryGet(
        Guid jobId,
        out JobSnapshot?
            snapshot)
    {
        if (_entries.TryGetValue(
                jobId,
                out Entry?
                    entry))
        {
            snapshot =
                entry.Snapshot;

            return true;
        }

        snapshot =
            null;

        return false;
    }

    public bool Remove(
        Guid jobId) =>
            _entries.TryRemove(
                jobId,
                out _);

    public async Task<JobSnapshot>
        WaitForTerminalAsync(
            Guid jobId,
            CancellationToken
                cancellationToken =
                    default)
    {
        if (!_entries.TryGetValue(
                jobId,
                out Entry?
                    entry))
        {
            throw new
                KeyNotFoundException(
                    $"Job '{jobId}' was not found.");
        }

        if (IsTerminal(
                entry.Snapshot.State))
        {
            return entry.Snapshot;
        }

        return await entry
            .Completion
            .Task
            .WaitAsync(
                cancellationToken);
    }

    private void Update(
        Guid jobId,
        Func<
            JobSnapshot,
            JobSnapshot>
            update)
    {
        if (!_entries.TryGetValue(
                jobId,
                out Entry?
                    entry))
        {
            throw new
                KeyNotFoundException(
                    $"Job '{jobId}' was not found.");
        }

        lock (entry.SyncRoot)
        {
            entry.Snapshot =
                update(
                    entry.Snapshot);
        }
    }

    private void Complete(
        Guid jobId,
        Func<
            JobSnapshot,
            JobSnapshot>
            update)
    {
        if (!_entries.TryGetValue(
                jobId,
                out Entry?
                    entry))
        {
            throw new
                KeyNotFoundException(
                    $"Job '{jobId}' was not found.");
        }

        JobSnapshot completed;

        lock (entry.SyncRoot)
        {
            if (IsTerminal(
                    entry.Snapshot.State))
            {
                return;
            }

            completed =
                update(
                    entry.Snapshot);

            entry.Snapshot =
                completed;
        }

        entry.Completion
            .TrySetResult(
                completed);
    }

    private static bool IsTerminal(
        JobState state) =>
            state
            is JobState.Succeeded
            or JobState.Failed
            or JobState.Canceled;

    private sealed class Entry(
        JobSnapshot snapshot)
    {
        public object SyncRoot
        {
            get;
        } = new();

        public JobSnapshot Snapshot
        {
            get;
            set;
        } = snapshot;

        public TaskCompletionSource<
            JobSnapshot>
            Completion
        {
            get;
        } =
            new(
                TaskCreationOptions
                    .RunContinuationsAsynchronously);
    }
}