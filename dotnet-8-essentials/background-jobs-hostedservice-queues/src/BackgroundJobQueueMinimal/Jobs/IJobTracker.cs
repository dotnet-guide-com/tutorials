namespace BackgroundJobQueueMinimal.Jobs;

public interface IJobTracker
{
    void Register(
        EmailJob job);

    void MarkRunning(
        Guid jobId);

    void MarkSucceeded(
        Guid jobId);

    void MarkFailed(
        Guid jobId,
        string failureCode,
        string failureMessage);

    void MarkCanceled(
        Guid jobId);

    bool TryGet(
        Guid jobId,
        out JobSnapshot?
            snapshot);

    bool Remove(
        Guid jobId);

    Task<JobSnapshot>
        WaitForTerminalAsync(
            Guid jobId,
            CancellationToken
                cancellationToken =
                    default);
}