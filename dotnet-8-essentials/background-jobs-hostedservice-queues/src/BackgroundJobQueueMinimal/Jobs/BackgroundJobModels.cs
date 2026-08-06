namespace BackgroundJobQueueMinimal.Jobs;

public sealed class BackgroundJobQueueOptions
{
    public const string SectionName =
        "BackgroundJobs";

    public int Capacity
    {
        get;
        set;
    } = 4;
}

public sealed record EmailJobRequest(
    string? To,
    string? Subject);

public sealed record EmailJob(
    Guid JobId,
    string To,
    string Subject,
    DateTimeOffset EnqueuedAt);

public enum JobState
{
    Queued,
    Running,
    Succeeded,
    Failed,
    Canceled
}

public sealed record JobSnapshot(
    Guid JobId,
    string JobName,
    JobState State,
    DateTimeOffset EnqueuedAt,
    string? FailureCode = null,
    string? FailureMessage = null);

public sealed record QueueSnapshot(
    int Capacity,
    int Depth,
    bool Accepting);