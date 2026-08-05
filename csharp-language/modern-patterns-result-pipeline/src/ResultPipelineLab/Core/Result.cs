namespace ResultPipelineLab.Core;

public sealed record PipelineError(
    string Code,
    string Message,
    string? Detail = null)
{
    public static PipelineError Parse(
        string code,
        string message,
        string? detail = null) =>
            new(
                code,
                message,
                detail);

    public static PipelineError Validation(
        string code,
        string message,
        string? detail = null) =>
            new(
                code,
                message,
                detail);

    public static PipelineError Storage(
        string code,
        string message,
        string? detail = null) =>
            new(
                code,
                message,
                detail);

    public PublicError ToPublic() =>
        new(
            Code,
            Message);

    public DiagnosticError ToDiagnostic(
        string stage)
    {
        ArgumentException
            .ThrowIfNullOrWhiteSpace(
                stage);

        return new DiagnosticError(
            Stage:
                stage,

            Code:
                Code,

            Message:
                Message,

            Detail:
                Detail
                ?? "none");
    }
}

public sealed record PublicError(
    string Code,
    string Message);

public sealed record DiagnosticError(
    string Stage,
    string Code,
    string Message,
    string Detail);

public abstract record Result<T>
{
    private Result()
    {
    }

    public sealed record Success(
        T Value) :
        Result<T>;

    public sealed record Failure(
        PipelineError Error) :
        Result<T>;

    public static Result<T> Ok(
        T value) =>
            new Success(
                value);

    public static Result<T> Fail(
        PipelineError error)
    {
        ArgumentNullException
            .ThrowIfNull(
                error);

        return new Failure(
            error);
    }
}