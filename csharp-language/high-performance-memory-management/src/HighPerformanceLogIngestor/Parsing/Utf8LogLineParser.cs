using System.Buffers;
using System.Buffers.Text;
using System.Text;
using HighPerformanceLogIngestor.Models;

namespace HighPerformanceLogIngestor.Parsing;

public sealed class Utf8LogLineParser
{
    private const long MinimumUnixSeconds =
        -62_135_596_800;

    private const long MaximumUnixSeconds =
        253_402_300_799;

    public bool TryParse(
        ReadOnlySpan<byte> line,
        out LogEntry? entry)
    {
        entry =
            null;

        int firstDelimiter =
            line.IndexOf(
                (byte)'|');

        if (firstDelimiter <= 0)
        {
            return false;
        }

        ReadOnlySpan<byte> remainder =
            line[
                (firstDelimiter + 1)..];

        int secondRelative =
            remainder.IndexOf(
                (byte)'|');

        if (secondRelative <= 0)
        {
            return false;
        }

        int secondDelimiter =
            firstDelimiter
            + 1
            + secondRelative;

        remainder =
            line[
                (secondDelimiter + 1)..];

        int thirdRelative =
            remainder.IndexOf(
                (byte)'|');

        if (thirdRelative <= 0)
        {
            return false;
        }

        int thirdDelimiter =
            secondDelimiter
            + 1
            + thirdRelative;

        ReadOnlySpan<byte> timestampBytes =
            line[
                ..firstDelimiter];

        ReadOnlySpan<byte> levelBytes =
            line[
                (firstDelimiter + 1)
                ..secondDelimiter];

        ReadOnlySpan<byte> eventIdBytes =
            line[
                (secondDelimiter + 1)
                ..thirdDelimiter];

        ReadOnlySpan<byte> messageBytes =
            line[
                (thirdDelimiter + 1)..];

        if (!TryParseInt64Exact(
                timestampBytes,
                out long unixSeconds)
            ||
            unixSeconds
                is < MinimumUnixSeconds
                or > MaximumUnixSeconds)
        {
            return false;
        }

        if (!TryParseByteExact(
                levelBytes,
                out byte level)
            ||
            level > 5)
        {
            return false;
        }

        if (!TryParseInt32Exact(
                eventIdBytes,
                out int eventId)
            ||
            eventId <= 0)
        {
            return false;
        }

        if (messageBytes.IsEmpty
            ||
            !IsValidUtf8(
                messageBytes))
        {
            return false;
        }

        entry =
            new LogEntry(
                Timestamp:
                    DateTimeOffset
                        .FromUnixTimeSeconds(
                            unixSeconds),

                Level:
                    level,

                EventId:
                    eventId,

                Message:
                    Encoding.UTF8
                        .GetString(
                            messageBytes));

        return true;
    }

    private static bool TryParseInt64Exact(
        ReadOnlySpan<byte> value,
        out long result) =>
            Utf8Parser.TryParse(
                value,
                out result,
                out int consumed)
            &&
            consumed
            == value.Length;

    private static bool TryParseInt32Exact(
        ReadOnlySpan<byte> value,
        out int result) =>
            Utf8Parser.TryParse(
                value,
                out result,
                out int consumed)
            &&
            consumed
            == value.Length;

    private static bool TryParseByteExact(
        ReadOnlySpan<byte> value,
        out byte result) =>
            Utf8Parser.TryParse(
                value,
                out result,
                out int consumed)
            &&
            consumed
            == value.Length;

    private static bool IsValidUtf8(
        ReadOnlySpan<byte> value)
    {
        while (!value.IsEmpty)
        {
            OperationStatus status =
                Rune.DecodeFromUtf8(
                    value,
                    out _,
                    out int consumed);

            if (status
                != OperationStatus.Done)
            {
                return false;
            }

            value =
                value[consumed..];
        }

        return true;
    }
}