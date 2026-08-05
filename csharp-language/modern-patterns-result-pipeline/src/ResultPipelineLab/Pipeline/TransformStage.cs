using ResultPipelineLab.Models;

namespace ResultPipelineLab.Pipeline;

public static class TransformStage
{
    public static ImportedProduct[]
        ToProducts(
            ValidatedProduct[] records)
    {
        ArgumentNullException
            .ThrowIfNull(
                records);

        return
        [
            .. records.Select(
                record =>
                    new ImportedProduct(
                        Name:
                            NormalizeName(
                                record.Name),

                        Price:
                            Math.Round(
                                record.Price,
                                2,
                                MidpointRounding
                                    .AwayFromZero),

                        Category:
                            record.Category,

                        Stock:
                            record.Stock))
        ];
    }

    private static string NormalizeName(
        string name) =>
            string.Join(
                ' ',
                name
                    .Split(
                        ' ',
                        StringSplitOptions
                            .RemoveEmptyEntries)
                    .Select(
                        word =>
                            char.ToUpperInvariant(
                                word[0])
                            + word[1..]
                                .ToLowerInvariant()));
}