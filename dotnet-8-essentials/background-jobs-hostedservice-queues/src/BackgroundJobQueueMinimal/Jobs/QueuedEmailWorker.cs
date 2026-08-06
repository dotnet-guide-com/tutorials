using BackgroundJobQueueMinimal.Services;

namespace BackgroundJobQueueMinimal.Jobs;

public sealed class QueuedEmailWorker(
    IBackgroundJobQueue queue,
    IJobTracker tracker,
    IServiceScopeFactory
        scopeFactory,
    ILogger<
        QueuedEmailWorker>
        logger) :
    BackgroundService
{
    protected override async Task
        ExecuteAsync(
            CancellationToken
                stoppingToken)
    {
        logger.LogInformation(
            "Background email worker started");

        await foreach (
            EmailJob job
            in queue.ReadAllAsync(
                stoppingToken))
        {
            tracker.MarkRunning(
                job.JobId);

            try
            {
                await using
                    AsyncServiceScope scope =
                        scopeFactory
                            .CreateAsyncScope();

                IEmailJobHandler handler =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IEmailJobHandler>();

                await handler.HandleAsync(
                    job,
                    stoppingToken);

                tracker.MarkSucceeded(
                    job.JobId);

                logger.LogInformation(
                    "Background job {JobId} succeeded",
                    job.JobId);
            }
            catch (
                OperationCanceledException)
                when (
                    stoppingToken
                        .IsCancellationRequested)
            {
                tracker.MarkCanceled(
                    job.JobId);

                throw;
            }
            catch (Exception exception)
            {
                tracker.MarkFailed(
                    job.JobId,
                    "JOB_PROCESSING_FAILED",
                    "Job processing failed.");

                logger.LogError(
                    exception,
                    "Background job {JobId} failed",
                    job.JobId);
            }
        }

        logger.LogInformation(
            "Background email worker stopped");
    }

    public override async Task StopAsync(
        CancellationToken
            cancellationToken)
    {
        queue.Complete();

        await base.StopAsync(
            cancellationToken);
    }
}