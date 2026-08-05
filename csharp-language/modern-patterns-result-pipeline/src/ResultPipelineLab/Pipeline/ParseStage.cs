using ResultPipelineLab.Core;
using ResultPipelineLab.Models;

namespace ResultPipelineLab.Pipeline;

public static class ParseStage
{
    private static readonly string[]
        ExpectedHeader =
        [
            "Name",
            "Price",
            "Category",
            "Stock"
        ];

    public static Result<RawProductRow[]>
        Parse(
            string text)
    {
        if (string.IsNullOrWhiteSpace(
                text))
        {
            return Result<
                RawProductRow[]>.Fail(
                    PipelineError.Parse(
                        "PARSE_EMPTY_INPUT",
                        "Input text is empty."));
        }

        if (text.Contains(
                '"',
                StringComparison.Ordinal))
        {
            return Result<
                RawProductRow[]>.Fail(
                    PipelineError.Parse(
                        "PARSE_QUOTES_UNSUPPORTED",
                        "Quoted fields are not supported by this sample.",
                        "Use a reviewed CSV library when quoted or escaped fields are required."));
        }

        List<string> lines =
        [
        ];

        using var reader =
            new StringReader(
                text);

        while (reader.ReadLine()
            is string line)
        {
            lines.Add(
                line);
        }

        if (lines.Count
            == 0)
        {
            return Result<
                RawProductRow[]>.Fail(
                    PipelineError.Parse(
                        "PARSE_NO_LINES",
                        "No import lines were found."));
        }

        string[] header =
        [
            .. lines[0]
                .Split(
                    ',',
                    StringSplitOptions.None)
                .Select(
                    value =>
                        value.Trim())
        ];

        if (!header.SequenceEqual(
                ExpectedHeader,
                StringComparer.Ordinal))
        {
            return Result<
                RawProductRow[]>.Fail(
                    PipelineError.Parse(
                        "PARSE_INVALID_HEADER",
                        "The import header does not match the expected columns.",
                        $"Expected: {string.Join(", ", ExpectedHeader)}; received: {string.Join(", ", header)}"));
        }

        List<RawProductRow> records =
        [
        ];

        List<string> malformed =
        [
        ];

        for (int index = 1;
            index < lines.Count;
            index++)
        {
            int lineNumber =
                index
                + 1;

            string line =
                lines[index];

            if (string.IsNullOrWhiteSpace(
                    line))
            {
                malformed.Add(
                    $"Line {lineNumber}: empty row.");

                continue;
            }

            string[] columns =
            [
                .. line
                    .Split(
                        ',',
                        StringSplitOptions.None)
                    .Select(
                        value =>
                            value.Trim())
            ];

            if (columns.Length
                != 4)
            {
                malformed.Add(
                    $"Line {lineNumber}: expected 4 fields, received {columns.Length}.");

                continue;
            }

            records.Add(
                new RawProductRow(
                    LineNumber:
                        lineNumber,

                    Name:
                        columns[0],

                    PriceText:
                        columns[1],

                    Category:
                        columns[2],

                    StockText:
                        columns[3]));
        }

        if (malformed.Count
            > 0)
        {
            return Result<
                RawProductRow[]>.Fail(
                    PipelineError.Parse(
                        "PARSE_MALFORMED_ROWS",
                        $"{malformed.Count} row(s) could not be parsed.",
                        string.Join(
                            " ",
                            malformed)));
        }

        if (records.Count
            == 0)
        {
            return Result<
                RawProductRow[]>.Fail(
                    PipelineError.Parse(
                        "PARSE_NO_DATA_ROWS",
                        "The import contains no product rows."));
        }

        return Result<
            RawProductRow[]>.Ok(
                [
                    .. records
                ]);
    }
}