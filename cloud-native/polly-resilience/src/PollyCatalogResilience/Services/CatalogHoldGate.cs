namespace PollyCatalogResilience.Services;

public sealed class CatalogHoldGate
{
    private readonly
        TaskCompletionSource<bool>
        _entered =
            new(
                TaskCreationOptions
                    .RunContinuationsAsynchronously);

    private readonly
        TaskCompletionSource<bool>
        _released =
            new(
                TaskCreationOptions
                    .RunContinuationsAsynchronously);

    public Task Entered =>
        _entered.Task;

    public async ValueTask WaitAsync(
        CancellationToken cancellationToken)
    {
        _entered.TrySetResult(
            true);

        await _released.Task
            .WaitAsync(
                cancellationToken);
    }

    public void Release()
    {
        _released.TrySetResult(
            true);
    }
}