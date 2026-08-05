using System.Globalization;
using ResultPipelineLab.Core;
using ResultPipelineLab.Models;

namespace ResultPipelineLab.Pipeline;

public static class ValidateStage
{
    private static readonly
        Dictionary<string, string>
        KnownCategories =
            new(
                StringComparer
                    .OrdinalIgnoreCase)
            {
                ["Electronics"] =
                    "Electronics",

                ["Books"] =
                    "Books",

                ["Hardware"] =
                    "Hardware"
            };

    private const NumberStyles
        DecimalStyles =
            NumberStyles.AllowLeadingSign
            | NumberStyles.AllowDecimalPoint;

    public static Result<
        ValidatedProduct[]>
        ValidateBatch(
            RawProductRow[] records)
    {
        ArgumentNullException
            .ThrowIfNull(
                records);

        if (records.Length
            == 0)
        {
            return Result<
                ValidatedProduct[]>.Fail(
                    PipelineError.Validation(
                        "VALIDATE_EMPTY_BATCH",
                        "There are no records to validate."));
        }

        List<ValidatedProduct> valid =
        [
        ];

        List<string> details =
        [
        ];

        int invalidRows =
            0;

        foreach (RawProductRow raw
            in records)
        {
            List<string> rowIssues =
            [
            ];

            string name =
                raw.Name.Trim();

            if (name.Length
                == 0)
            {
                rowIssues.Add(
                    "Name is required.");
            }
            else if (name.Length
                > 100)
            {
                rowIssues.Add(
                    $"Name exceeds 100 characters ({name.Length}).");
            }

            bool priceValid =
                decimal.TryParse(
                    raw.PriceText,
                    DecimalStyles,
                    CultureInfo
                        .InvariantCulture,
                    out decimal price);

            if (!priceValid)
            {
                rowIssues.Add(
                    $"Price '{raw.PriceText}' is not an invariant decimal.");
            }
            else if (price
                <= 0)
            {
                rowIssues.Add(
                    $"Price must be greater than zero (received {price.ToString(CultureInfo.InvariantCulture)}).");
            }

            bool stockValid =
                int.TryParse(
                    raw.StockText,
                    NumberStyles.Integer,
                    CultureInfo
                        .InvariantCulture,
                    out int stock);

            if (!stockValid)
            {
                rowIssues.Add(
                    $"Stock '{raw.StockText}' is not an invariant integer.");
            }
            else if (stock
                < 0)
            {
                rowIssues.Add(
                    $"Stock cannot be negative (received {stock}).");
            }

            bool categoryValid =
                KnownCategories.TryGetValue(
                    raw.Category.Trim(),
                    out string?
                        canonicalCategory);

            if (!categoryValid)
            {
                rowIssues.Add(
                    $"Category '{raw.Category}' is not supported.");
            }

            if (rowIssues.Count
                > 0)
            {
                invalidRows++;

                details.Add(
                    $"Line {raw.LineNumber}: {string.Join(" ", rowIssues)}");

                continue;
            }

            valid.Add(
                new ValidatedProduct(
                    LineNumber:
                        raw.LineNumber,

                    Name:
                        name,

                    Price:
                        price,

                    Category:
                        canonicalCategory!,

                    Stock:
                        stock));
        }

        if (invalidRows
            > 0)
        {
            return Result<
                ValidatedProduct[]>.Fail(
                    PipelineError.Validation(
                        "VALIDATE_BATCH_FAILED",
                        $"{invalidRows} record(s) failed validation.",
                        string.Join(
                            " ",
                            details)));
        }

        return Result<
            ValidatedProduct[]>.Ok(
                [
                    .. valid
                ]);
    }
}