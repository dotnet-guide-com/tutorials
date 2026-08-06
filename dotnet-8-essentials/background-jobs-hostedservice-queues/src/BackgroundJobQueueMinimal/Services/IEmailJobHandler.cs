using BackgroundJobQueueMinimal.Jobs;

namespace BackgroundJobQueueMinimal.Services;

public interface IEmailJobHandler
{
    Task HandleAsync(
        EmailJob job,
        CancellationToken
            cancellationToken);
}