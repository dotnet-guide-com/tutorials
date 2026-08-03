using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ResilientOrdersMinimal.Health;

public sealed class TrafficReadinessHealthCheck(
    TrafficReadinessState state) :
    IHealthCheck
{
    public Task<HealthCheckResult>
        CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken =
                default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        HealthCheckResult result =
            state.IsAcceptingTraffic
                ? HealthCheckResult.Healthy(
                    "The API is accepting traffic.")
                : HealthCheckResult.Unhealthy(
                    "The API is draining and must not receive new traffic.");

        return Task.FromResult(
            result);
    }
}