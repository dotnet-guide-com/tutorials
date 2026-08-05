using ResultPipelineLab.Core;
using ResultPipelineLab.Models;
using ResultPipelineLab.Persistence;
using ResultPipelineLab.Pipeline;

namespace ResultPipelineLab.Tests;

public sealed class ResultPipelineTests
{
    private const string ValidInput =
        """
        Name,Price,Category,Stock
        widget   pro,9.99,electronics,50
        travel mug,14.50,hardware,100
        """;

    [Fact]
    public void
        Map_and_bind_transform_success()
    {
        Result<string> source =
            Result<string>.Ok(
                " 42 ");

        Result<int> result =
            source
                .Map(
                    value =>
                        value.Trim())
                .Bind(
                    value =>
                        int.TryParse(
                            value,
                            out int parsed)
                                ? Result<int>
                                    .Ok(
                                        parsed)
                                : Result<int>
                                    .Fail(
                                        PipelineError
                                            .Parse(
                                                "PARSE_INT",
                                                "The value is not an integer.")));

        Result<int>.Success success =
            Assert.IsType<
                Result<int>.Success>(
                    result);

        Assert.Equal(
            42,
            success.Value);
    }

    [Fact]
    public async Task
        Failure_short_circuits_sync_and_async_steps()
    {
        bool mapCalled =
            false;

        bool bindCalled =
            false;

        bool asyncCalled =
            false;

        Result<string> failure =
            Result<string>.Fail(
                PipelineError.Validation(
                    "INPUT_INVALID",
                    "The input is invalid."));

        Result<int> syncResult =
            failure
                .Map(
                    value =>
                    {
                        mapCalled =
                            true;

                        return value.Length;
                    })
                .Bind(
                    value =>
                    {
                        bindCalled =
                            true;

                        return Result<int>
                            .Ok(
                                value);
                    });

        Result<int> asyncResult =
            await syncResult
                .BindAsync(
                    (
                        value,
                        _) =>
                    {
                        asyncCalled =
                            true;

                        return Task.FromResult(
                            Result<int>.Ok(
                                value));
                    },

                    TestContext.Current
                        .CancellationToken);

        Assert.False(
            mapCalled);

        Assert.False(
            bindCalled);

        Assert.False(
            asyncCalled);

        Result<int>.Failure final =
            Assert.IsType<
                Result<int>.Failure>(
                    asyncResult);

        Assert.Equal(
            "INPUT_INVALID",
            final.Error.Code);
    }

    [Fact]
    public void
        Match_and_error_projections_separate_public_and_internal_data()
    {
        PipelineError error =
            PipelineError.Validation(
                "VALIDATE_BATCH_FAILED",
                "One record failed validation.",
                "Line 2: raw value was SECRET-123.");

        Result<int> failure =
            Result<int>.Fail(
                error);

        string matched =
            failure.Match(
                onSuccess:
                    value =>
                        value.ToString(),

                onFailure:
                    current =>
                        current.Code);

        PublicError publicError =
            error.ToPublic();

        DiagnosticError diagnostic =
            error.ToDiagnostic(
                "Validate");

        Assert.Equal(
            "VALIDATE_BATCH_FAILED",
            matched);

        Assert.Equal(
            "One record failed validation.",
            publicError.Message);

        Assert.DoesNotContain(
            "SECRET-123",
            publicError.Message,
            StringComparison.Ordinal);

        Assert.Contains(
            "SECRET-123",
            diagnostic.Detail,
            StringComparison.Ordinal);
    }

    [Fact]
    public void
        Parse_stage_accepts_restricted_rows()
    {
        Result<RawProductRow[]> result =
            ParseStage.Parse(
                ValidInput);

        Result<RawProductRow[]>.Success
            success =
                Assert.IsType<
                    Result<RawProductRow[]>
                        .Success>(
                            result);

        Assert.Collection(
            success.Value,
            first =>
            {
                Assert.Equal(
                    2,
                    first.LineNumber);

                Assert.Equal(
                    "widget   pro",
                    first.Name);
            },
            second =>
            {
                Assert.Equal(
                    3,
                    second.LineNumber);

                Assert.Equal(
                    "14.50",
                    second.PriceText);
            });
    }

    [Fact]
    public void
        Parse_stage_rejects_quotes_and_malformed_rows()
    {
        const string quoted =
            """
            Name,Price,Category,Stock
            "Widget, Pro",9.99,Electronics,10
            """;

        Result<RawProductRow[]>.Failure
            quoteFailure =
                Assert.IsType<
                    Result<RawProductRow[]>
                        .Failure>(
                            ParseStage.Parse(
                                quoted));

        Assert.Equal(
            "PARSE_QUOTES_UNSUPPORTED",
            quoteFailure.Error.Code);

        const string malformed =
            """
            Name,Price,Category,Stock
            Widget,9.99,Electronics
            Travel Mug,14.50,Hardware
            """;

        Result<RawProductRow[]>.Failure
            rowFailure =
                Assert.IsType<
                    Result<RawProductRow[]>
                        .Failure>(
                            ParseStage.Parse(
                                malformed));

        Assert.Equal(
            "PARSE_MALFORMED_ROWS",
            rowFailure.Error.Code);

        Assert.Equal(
            "2 row(s) could not be parsed.",
            rowFailure.Error.Message);

        Assert.Contains(
            "Line 2",
            rowFailure.Error.Detail,
            StringComparison.Ordinal);

        Assert.Contains(
            "Line 3",
            rowFailure.Error.Detail,
            StringComparison.Ordinal);
    }

    [Fact]
    public void
        Validation_collects_all_invalid_rows()
    {
        RawProductRow[] rows =
        [
            new RawProductRow(
                2,
                "",
                "-5",
                "Electronics",
                "10"),

            new RawProductRow(
                3,
                "Book",
                "12.50",
                "Unknown",
                "-1"),

            new RawProductRow(
                4,
                "Valid",
                "9.99",
                "Books",
                "2")
        ];

        Result<ValidatedProduct[]>.Failure
            failure =
                Assert.IsType<
                    Result<ValidatedProduct[]>
                        .Failure>(
                            ValidateStage
                                .ValidateBatch(
                                    rows));

        Assert.Equal(
            "VALIDATE_BATCH_FAILED",
            failure.Error.Code);

        Assert.Equal(
            "2 record(s) failed validation.",
            failure.Error.Message);

        Assert.Contains(
            "Line 2",
            failure.Error.Detail,
            StringComparison.Ordinal);

        Assert.Contains(
            "Line 3",
            failure.Error.Detail,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "Line 4",
            failure.Error.Detail,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task
        Pipeline_success_transforms_and_persists()
    {
        var store =
            new InMemoryProductStore();

        var pipeline =
            new ImportPipeline(
                store);

        Result<ImportSummary> result =
            await pipeline.RunAsync(
                ValidInput,
                TestContext.Current
                    .CancellationToken);

        Result<ImportSummary>.Success
            success =
                Assert.IsType<
                    Result<ImportSummary>
                        .Success>(
                            result);

        Assert.Equal(
            2,
            success.Value
                .TotalInserted);

        Assert.Equal(
            1,
            store.WriteAttempts);

        Assert.Collection(
            store.Products,
            first =>
            {
                Assert.Equal(
                    "Widget Pro",
                    first.Name);

                Assert.Equal(
                    9.99m,
                    first.Price);

                Assert.Equal(
                    "Electronics",
                    first.Category);
            },
            second =>
            {
                Assert.Equal(
                    "Travel Mug",
                    second.Name);

                Assert.Equal(
                    100,
                    second.Stock);
            });
    }

    [Fact]
    public async Task
        Parse_and_validation_failures_do_not_write()
    {
        var parseStore =
            new InMemoryProductStore();

        var parsePipeline =
            new ImportPipeline(
                parseStore);

        Result<ImportSummary>.Failure
            parseFailure =
                Assert.IsType<
                    Result<ImportSummary>
                        .Failure>(
                            await parsePipeline
                                .RunAsync(
                                    "",
                                    TestContext
                                        .Current
                                        .CancellationToken));

        Assert.Equal(
            "PARSE_EMPTY_INPUT",
            parseFailure.Error.Code);

        Assert.Equal(
            0,
            parseStore.WriteAttempts);

        var validationStore =
            new InMemoryProductStore();

        var validationPipeline =
            new ImportPipeline(
                validationStore);

        const string invalid =
            """
            Name,Price,Category,Stock
            Broken,-5,Electronics,1
            """;

        Result<ImportSummary>.Failure
            validationFailure =
                Assert.IsType<
                    Result<ImportSummary>
                        .Failure>(
                            await validationPipeline
                                .RunAsync(
                                    invalid,
                                    TestContext
                                        .Current
                                        .CancellationToken));

        Assert.Equal(
            "VALIDATE_BATCH_FAILED",
            validationFailure
                .Error.Code);

        Assert.Equal(
            0,
            validationStore
                .WriteAttempts);
    }

    [Fact]
    public async Task
        Storage_failure_is_returned_as_data()
    {
        var store =
            new InMemoryProductStore(
                rejectWrites:
                    true);

        var pipeline =
            new ImportPipeline(
                store);

        Result<ImportSummary>.Failure
            failure =
                Assert.IsType<
                    Result<ImportSummary>
                        .Failure>(
                            await pipeline
                                .RunAsync(
                                    ValidInput,
                                    TestContext
                                        .Current
                                        .CancellationToken));

        Assert.Equal(
            "STORE_WRITE_REJECTED",
            failure.Error.Code);

        Assert.Equal(
            1,
            store.WriteAttempts);

        Assert.Empty(
            store.Products);
    }

    [Fact]
    public async Task
        Caller_cancellation_propagates_without_write()
    {
        var store =
            new InMemoryProductStore();

        var pipeline =
            new ImportPipeline(
                store);

        using var cancellation =
            new CancellationTokenSource();

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
                () =>
                    pipeline.RunAsync(
                        ValidInput,
                        cancellation.Token));

        Assert.Equal(
            0,
            store.WriteAttempts);
    }
}