using System.Diagnostics;

namespace ResultPipelineLab.Core;

public static class ResultExtensions
{
    public static Result<TNext> Map<
        T,
        TNext>(
        this Result<T> result,
        Func<T, TNext> transform)
    {
        ArgumentNullException
            .ThrowIfNull(
                result);

        ArgumentNullException
            .ThrowIfNull(
                transform);

        return result switch
        {
            Result<T>.Success success =>
                Result<TNext>.Ok(
                    transform(
                        success.Value)),

            Result<T>.Failure failure =>
                Result<TNext>.Fail(
                    failure.Error),

            _ =>
                throw new UnreachableException(
                    "Unknown Result case.")
        };
    }

    public static Result<TNext> Bind<
        T,
        TNext>(
        this Result<T> result,
        Func<T, Result<TNext>> next)
    {
        ArgumentNullException
            .ThrowIfNull(
                result);

        ArgumentNullException
            .ThrowIfNull(
                next);

        return result switch
        {
            Result<T>.Success success =>
                next(
                    success.Value),

            Result<T>.Failure failure =>
                Result<TNext>.Fail(
                    failure.Error),

            _ =>
                throw new UnreachableException(
                    "Unknown Result case.")
        };
    }

    public static TOut Match<
        T,
        TOut>(
        this Result<T> result,
        Func<T, TOut> onSuccess,
        Func<PipelineError, TOut> onFailure)
    {
        ArgumentNullException
            .ThrowIfNull(
                result);

        ArgumentNullException
            .ThrowIfNull(
                onSuccess);

        ArgumentNullException
            .ThrowIfNull(
                onFailure);

        return result switch
        {
            Result<T>.Success success =>
                onSuccess(
                    success.Value),

            Result<T>.Failure failure =>
                onFailure(
                    failure.Error),

            _ =>
                throw new UnreachableException(
                    "Unknown Result case.")
        };
    }

    public static Task<Result<TNext>>
        BindAsync<
            T,
            TNext>(
            this Result<T> result,
            Func<
                T,
                CancellationToken,
                Task<Result<TNext>>>
                next,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException
            .ThrowIfNull(
                result);

        ArgumentNullException
            .ThrowIfNull(
                next);

        return result switch
        {
            Result<T>.Success success =>
                next(
                    success.Value,
                    cancellationToken),

            Result<T>.Failure failure =>
                Task.FromResult(
                    Result<TNext>.Fail(
                        failure.Error)),

            _ =>
                throw new UnreachableException(
                    "Unknown Result case.")
        };
    }
}