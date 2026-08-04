using Polly;
using PollyCatalogResilience.Models;
using PollyCatalogResilience.Services;

namespace PollyCatalogResilience.Resilience;

public sealed class CatalogResilienceService(
    ResiliencePipeline<
        CatalogSnapshot> pipeline,
    CatalogDependency dependency)
{
    public ValueTask<CatalogSnapshot>
        GetSnapshotAsync(
            CatalogSimulationMode mode,
            int delayMilliseconds,
            CancellationToken cancellationToken)
    {
        var state =
            new CatalogExecutionState(
                dependency,
                mode,
                delayMilliseconds);

        return pipeline.ExecuteAsync(
            static (
                execution,
                token) =>
                    execution
                        .Dependency
                        .GetSnapshotAsync(
                            execution.Mode,
                            execution
                                .DelayMilliseconds,
                            token),

            state,
            cancellationToken);
    }

    private readonly record struct
        CatalogExecutionState(
            CatalogDependency Dependency,
            CatalogSimulationMode Mode,
            int DelayMilliseconds);
}