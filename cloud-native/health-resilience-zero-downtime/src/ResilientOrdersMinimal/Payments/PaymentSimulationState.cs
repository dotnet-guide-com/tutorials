using System.Collections.Concurrent;

namespace ResilientOrdersMinimal.Payments;

public sealed class PaymentSimulationState
{
    private readonly
        ConcurrentDictionary<
            string,
            int> _attempts =
                new(
                    StringComparer.Ordinal);

    public int RecordAttempt(
        string operationId) =>
            _attempts.AddOrUpdate(
                operationId,
                addValue:
                    1,
                updateValueFactory:
                    static (
                        key,
                        current) =>
                            current + 1);

    public int GetAttempts(
        string operationId) =>
            _attempts.TryGetValue(
                operationId,
                out int attempts)
                ? attempts
                : 0;

    public void Remove(
        string operationId)
    {
        _attempts.TryRemove(
            operationId,
            out _);
    }
}