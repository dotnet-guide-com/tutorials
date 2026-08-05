using System.Buffers;
using System.IO.Pipelines;
using HighPerformanceLogIngestor.Models;
using HighPerformanceLogIngestor.Parsing;

namespace HighPerformanceLogIngestor.Pipelines;

public sealed class LogIngestor(
    LogLineDecoder decoder)
{
    // The maximum bytes of an incomplete record retained while waiting
    // for a newline before triggering oversized-record discard.
    // Set to MaximumLineLength + 1 so a valid maximum-length record
    // can be followed by CR before LF.
    private const int MaxRetainedBeforeNewline =
        LogLineDecoder.MaximumLineLength
        + 1;

    public async Task<LogIngestionResult>
        IngestAsync(
            Stream stream,
            Action<LogEntry> onEntry,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException
            .ThrowIfNull(
                stream);

        ArgumentNullException
            .ThrowIfNull(
                onEntry);

        if (!stream.CanRead)
        {
            throw new ArgumentException(
                "The stream must be readable.",
                nameof(stream));
        }

        var options =
            new StreamPipeReaderOptions(
                leaveOpen:
                    true);

        PipeReader reader =
            PipeReader.Create(
                stream,
                options);

        int totalLines =
            0;

        int validLines =
            0;

        int invalidLines =
            0;

        int pooledCopies =
            0;

        // Running count of incomplete-record bytes seen across
        // PipeReader reads when no LF is present in the buffer.
        long incompleteBytesSeen =
            0;

        bool discardingOversized =
            false;

        Exception? completionError =
            null;

        try
        {
            while (true)
            {
                ReadResult result =
                    await reader.ReadAsync(
                        cancellationToken);

                ReadOnlySequence<byte> buffer =
                    result.Buffer;

                if (result.IsCanceled)
                {
                    reader.AdvanceTo(
                        buffer.Start,
                        buffer.End);

                    cancellationToken
                        .ThrowIfCancellationRequested();

                    throw new OperationCanceledException(
                        "The pipeline read was canceled.");
                }

                while (TryReadLine(
                    ref buffer,
                    out ReadOnlySequence<byte>
                        line))
                {
                    incompleteBytesSeen =
                        0;

                    if (discardingOversized)
                    {
                        // This LF ends the oversized-discard cycle.
                        discardingOversized =
                            false;

                        continue;
                    }

                    ProcessLine(
                        line);
                }

                if (discardingOversized)
                {
                    if (result.IsCompleted)
                    {
                        // Stream ended without an LF; the oversized
                        // record was already counted once.
                        reader.AdvanceTo(
                            result.Buffer.End);

                        break;
                    }

                    // Consume all bytes and keep looking for LF.
                    incompleteBytesSeen =
                        0;

                    reader.AdvanceTo(
                        buffer.End);

                    continue;
                }

                if (!buffer.IsEmpty)
                {
                    incompleteBytesSeen +=
                        buffer.Length;
                }

                if (incompleteBytesSeen
                    > MaxRetainedBeforeNewline)
                {
                    // The incomplete record exceeds the bound.
                    // Count it only once and enter discard mode.
                    totalLines++;
                    invalidLines++;

                    discardingOversized =
                        true;

                    incompleteBytesSeen =
                        0;

                    reader.AdvanceTo(
                        buffer.End);

                    continue;
                }

                if (result.IsCompleted)
                {
                    if (!buffer.IsEmpty)
                    {
                        ProcessLine(
                            buffer);
                    }

                    reader.AdvanceTo(
                        result.Buffer.End);

                    break;
                }

                reader.AdvanceTo(
                    buffer.Start,
                    buffer.End);
            }
        }
        catch (Exception exception)
        {
            completionError =
                exception;

            throw;
        }
        finally
        {
            await reader.CompleteAsync(
                completionError);
        }

        return new LogIngestionResult(
            TotalLines:
                totalLines,

            ValidLines:
                validLines,

            InvalidLines:
                invalidLines,

            PooledCopies:
                pooledCopies);

        void ProcessLine(
            ReadOnlySequence<byte> line)
        {
            totalLines++;

            bool decoded =
                decoder.TryDecode(
                    line,
                    out LogEntry? entry,
                    out bool usedPooledCopy);

            if (usedPooledCopy)
            {
                pooledCopies++;
            }

            if (decoded)
            {
                validLines++;

                onEntry(
                    entry!);
            }
            else
            {
                invalidLines++;
            }
        }
    }

    private static bool TryReadLine(
        ref ReadOnlySequence<byte> buffer,
        out ReadOnlySequence<byte> line)
    {
        SequencePosition? newline =
            buffer.PositionOf(
                (byte)'\n');

        if (newline is null)
        {
            line =
                default;

            return false;
        }

        line =
            buffer.Slice(
                0,
                newline.Value);

        SequencePosition next =
            buffer.GetPosition(
                1,
                newline.Value);

        buffer =
            buffer.Slice(
                next);

        return true;
    }
}