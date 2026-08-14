using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreRelationshipsMinimal.Diagnostics;

public sealed class
    SelectCountingInterceptor :
    DbCommandInterceptor
{
    private int _selectCount;

    public int SelectCount =>
        Volatile.Read(
            ref _selectCount);

    public void Reset() =>
        Interlocked.Exchange(
            ref _selectCount,
            0);

    public override
        InterceptionResult<
            DbDataReader>
        ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<
                DbDataReader> result)
    {
        CountSelect(
            command);

        return base.ReaderExecuting(
            command,
            eventData,
            result);
    }

    public override
        ValueTask<
            InterceptionResult<
                DbDataReader>>
        ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<
                DbDataReader> result,
            CancellationToken
                cancellationToken =
                    default)
    {
        CountSelect(
            command);

        return base
            .ReaderExecutingAsync(
                command,
                eventData,
                result,
                cancellationToken);
    }

    private void CountSelect(
        DbCommand command)
    {
        if (command.CommandText
            .TrimStart()
            .StartsWith(
                "SELECT",
                StringComparison
                    .OrdinalIgnoreCase))
        {
            Interlocked.Increment(
                ref _selectCount);
        }
    }
}