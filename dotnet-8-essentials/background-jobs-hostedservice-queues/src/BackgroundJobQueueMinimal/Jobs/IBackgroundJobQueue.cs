namespace BackgroundJobQueueMinimal.Jobs;

public interface IBackgroundJobQueue
{
    int Capacity
    {
        get;
    }

    int Depth
    {
        get;
    }

    bool IsAccepting
    {
        get;
    }

    ValueTask EnqueueAsync(
        EmailJob job,
        CancellationToken
            cancellationToken =
                default);

    IAsyncEnumerable<EmailJob>
        ReadAllAsync(
            CancellationToken
                cancellationToken =
                    default);

    void Complete();
}