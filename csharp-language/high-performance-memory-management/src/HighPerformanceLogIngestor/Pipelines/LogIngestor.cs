using System.Buffers;
using System.IO.Pipelines;
using HighPerformanceLogIngestor.Models;
using HighPerformanceLogIngestor.Parsing;

namespace HighPerformanceLogIngestor.Pipelines;

public sealed class LogIngestor(
    LogLineDecoder decoder)
{
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
                    ProcessLine(
                        line);
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

            if (decoder.TryDecode(
                line,
                out LogEntry? entry,
                out bool usedPooledCopy))
            {
                validLines++;

                if (usedPooledCopy)
                {
                    pooledCopies++;
                }

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