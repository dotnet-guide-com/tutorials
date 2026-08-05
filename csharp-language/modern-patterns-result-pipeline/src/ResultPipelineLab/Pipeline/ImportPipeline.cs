using ResultPipelineLab.Core;
using ResultPipelineLab.Models;
using ResultPipelineLab.Persistence;

namespace ResultPipelineLab.Pipeline;

public sealed class ImportPipeline(
    IProductStore store)
{
    private readonly IProductStore
        _store =
            store
            ?? throw new
                ArgumentNullException(
                    nameof(store));

    public async Task<
        Result<ImportSummary>>
        RunAsync(
            string text,
            CancellationToken
                cancellationToken =
                    default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        Result<ImportedProduct[]>
            prepared =
                ParseStage
                    .Parse(
                        text)
                    .Bind(
                        ValidateStage
                            .ValidateBatch)
                    .Map(
                        TransformStage
                            .ToProducts);

        return await prepared
            .BindAsync(
                (
                    products,
                    token) =>
                        _store.SaveAsync(
                            products,
                            token),

                cancellationToken);
    }
}