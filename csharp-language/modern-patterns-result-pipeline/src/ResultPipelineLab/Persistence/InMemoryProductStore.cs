using ResultPipelineLab.Core;
using ResultPipelineLab.Models;

namespace ResultPipelineLab.Persistence;

public interface IProductStore
{
    Task<Result<ImportSummary>>
        SaveAsync(
            IReadOnlyList<
                ImportedProduct>
                products,
            CancellationToken
                cancellationToken);
}

public sealed class InMemoryProductStore(
    bool rejectWrites = false) :
    IProductStore
{
    private readonly bool
        _rejectWrites =
            rejectWrites;

    private readonly
        List<ImportedProduct>
        _products =
        [
        ];

    public int WriteAttempts
    {
        get;
        private set;
    }

    public IReadOnlyList<
        ImportedProduct>
        Products =>
            [
                .. _products
            ];

    public Task<Result<ImportSummary>>
        SaveAsync(
            IReadOnlyList<
                ImportedProduct>
                products,
            CancellationToken
                cancellationToken)
    {
        ArgumentNullException
            .ThrowIfNull(
                products);

        cancellationToken
            .ThrowIfCancellationRequested();

        WriteAttempts++;

        if (_rejectWrites)
        {
            return Task.FromResult(
                Result<
                    ImportSummary>.Fail(
                        PipelineError.Storage(
                            "STORE_WRITE_REJECTED",
                            "The products could not be saved.",
                            "The deterministic in-memory store was configured to reject writes.")));
        }

        _products.AddRange(
            products);

        return Task.FromResult(
            Result<
                ImportSummary>.Ok(
                    new ImportSummary(
                        TotalInserted:
                            products.Count)));
    }
}