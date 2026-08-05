using System.Buffers;
using HighPerformanceLogIngestor.Models;

namespace HighPerformanceLogIngestor.Parsing;

public sealed class LogLineDecoder(
    Utf8LogLineParser parser)
{
    public const int MaximumLineLength =
        4_096;

    public bool TryDecode(
        ReadOnlySequence<byte> line,
        out LogEntry? entry,
        out bool usedPooledCopy)
    {
        entry =
            null;

        usedPooledCopy =
            false;

        line =
            TrimTrailingCarriageReturn(
                line);

        if (line.IsEmpty
            ||
            line.Length
                > MaximumLineLength)
        {
            return false;
        }

        if (line.IsSingleSegment)
        {
            return parser.TryParse(
                line.FirstSpan,
                out entry);
        }

        int length =
            checked(
                (int)line.Length);

        using IMemoryOwner<byte> owner =
            MemoryPool<byte>
                .Shared
                .Rent(
                    length);

        Span<byte> destination =
            owner.Memory
                .Span[
                    ..length];

        line.CopyTo(
            destination);

        usedPooledCopy =
            true;

        return parser.TryParse(
            destination,
            out entry);
    }

    private static ReadOnlySequence<byte>
        TrimTrailingCarriageReturn(
            ReadOnlySequence<byte> line)
    {
        if (line.IsEmpty)
        {
            return line;
        }

        ReadOnlySequence<byte> finalByte =
            line.Slice(
                line.Length
                - 1);

        return finalByte.FirstSpan[0]
            == (byte)'\r'
                ? line.Slice(
                    0,
                    line.Length
                    - 1)
                : line;
    }
}