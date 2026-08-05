using ResultPipelineLab.Core;
using ResultPipelineLab.Persistence;
using ResultPipelineLab.Pipeline;

const string ValidInput =
    """
    Name,Price,Category,Stock
    widget   pro,9.99,electronics,50
    travel mug,14.50,hardware,100
    """;

const string InvalidInput =
    """
    Name,Price,Category,Stock
    Broken Item,-5,Electronics,10
    """;

var successStore =
    new InMemoryProductStore();

var successPipeline =
    new ImportPipeline(
        successStore);

ResultPipelineLab.Core.Result<
    ResultPipelineLab.Models.ImportSummary>
    success =
        await successPipeline
            .RunAsync(
                ValidInput);

var failureStore =
    new InMemoryProductStore();

var failurePipeline =
    new ImportPipeline(
        failureStore);

ResultPipelineLab.Core.Result<
    ResultPipelineLab.Models.ImportSummary>
    failure =
        await failurePipeline
            .RunAsync(
                InvalidInput);

string successLine =
    success.Match(
        onSuccess:
            summary =>
                $"Success: imported={summary.TotalInserted}, writes={successStore.WriteAttempts}",

        onFailure:
            error =>
                $"Unexpected failure: {error.Code}");

string failureLine =
    failure.Match(
        onSuccess:
            summary =>
                $"Unexpected success: imported={summary.TotalInserted}",

        onFailure:
            error =>
                $"Failure: code={error.Code}, writes={failureStore.WriteAttempts}");

PublicError publicError =
    failure.Match(
        onSuccess:
            _ =>
                new PublicError(
                    "UNEXPECTED_SUCCESS",
                    "The invalid import unexpectedly succeeded."),

        onFailure:
            error =>
                error.ToPublic());

Console.WriteLine(
    "C# 12 Result Pipeline Lab");

Console.WriteLine(
    successLine);

Console.WriteLine(
    $"Products: {string.Join(" | ", successStore.Products.Select(product => product.Name))}");

Console.WriteLine(
    failureLine);

Console.WriteLine(
    $"Public message: {publicError.Message}");