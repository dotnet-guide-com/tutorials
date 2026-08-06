using BackgroundJobQueueMinimal.Jobs;

namespace BackgroundJobQueueMinimal.Services;

public sealed class FakeEmailJobHandler(
    ILogger<
        FakeEmailJobHandler>
        logger) :
    IEmailJobHandler
{
    public async Task HandleAsync(
        EmailJob job,
        CancellationToken
            cancellationToken)
    {
        ArgumentNullException
            .ThrowIfNull(
                job);

        await Task.Delay(
            TimeSpan
                .FromMilliseconds(
                    25),
            cancellationToken);

        logger.LogInformation(
            "Processed background email job {JobId}",
            job.JobId);
    }
}