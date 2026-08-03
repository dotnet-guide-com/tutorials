using ResilientOrdersMinimal.Health;

namespace ResilientOrdersMinimal.Hosting;

public sealed class ShutdownReadinessService(
    IHostApplicationLifetime lifetime,
    TrafficReadinessState readiness,
    ILogger<ShutdownReadinessService> logger) :
    IHostedService,
    IDisposable
{
    private CancellationTokenRegistration
        _stoppingRegistration;

    public Task StartAsync(
        CancellationToken cancellationToken)
    {
        _stoppingRegistration =
            lifetime.ApplicationStopping
                .Register(
                    () =>
                    {
                        readiness.BeginDrain();

                        logger.LogInformation(
                            "Application shutdown started; readiness is now unavailable.");
                    });

        return Task.CompletedTask;
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        readiness.BeginDrain();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _stoppingRegistration
            .Dispose();
    }
}