using System.Buffers;
using System.Text;
using HighPerformanceLogIngestor.Models;
using HighPerformanceLogIngestor.Parsing;
using HighPerformanceLogIngestor.Pipelines;
using HighPerformanceLogIngestor.Tests.TestSupport;

namespace HighPerformanceLogIngestor.Tests;

public sealed class HighPerformanceLogTests
{
    [Fact]
    public void
        Parser_reads_exact_utf8_fields()
    {
        var parser =
            new Utf8LogLineParser();

        bool success =
            parser.TryParse(
                "1785924000|2|1001|Cache warmed"u8,
                out LogEntry? entry);

        Assert.True(
            success);

        Assert.NotNull(
            entry);

        Assert.Equal(
            DateTimeOffset
                .FromUnixTimeSeconds(
                    1_785_924_000),
            entry.Timestamp);

        Assert.Equal(
            (byte)2,
            entry.Level);

        Assert.Equal(
            1001,
            entry.EventId);

        Assert.Equal(
            "Cache warmed",
            entry.Message);
    }

    [Fact]
    public void
        Parser_rejects_malformed_or_partial_fields()
    {
        var parser =
            new Utf8LogLineParser();

        byte[][] invalidLines =
        [
            "bad|2|1001|Message"u8
                .ToArray(),

            "1785924000|9|1001|Message"u8
                .ToArray(),

            "1785924000|2x|1001|Message"u8
                .ToArray(),

            "1785924000|2|1001x|Message"u8
                .ToArray(),

            "1785924000|2|0|Message"u8
                .ToArray(),

            "1785924000|2|1001|"u8
                .ToArray(),

            [
                .. "1785924000|2|1001|"u8,
                0xC3,
                0x28
            ]
        ];

        foreach (byte[] line in
            invalidLines)
        {
            Assert.False(
                parser.TryParse(
                    line,
                    out _));
        }

        // ChunkedReadStream constructor validation.
        Assert.Throws<ArgumentNullException>(
            () =>
                new ChunkedReadStream(
                    null!,
                    1));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new ChunkedReadStream(
                    [],
                    0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new ChunkedReadStream(
                    [],
                    -1));
    }

    [Fact]
    public void
        Decoder_uses_pooled_copy_for_multisegment_line()
    {
        var decoder =
            CreateDecoder();

        // Valid multi-segment line.
        ReadOnlySequence<byte> line =
            SegmentedSequence.Create(
                "1785924"u8
                    .ToArray(),

                "000|2|10"u8
                    .ToArray(),

                "01|Cache warmed"u8
                    .ToArray());

        bool success =
            decoder.TryDecode(
                line,
                out LogEntry? entry,
                out bool usedPooledCopy);

        Assert.True(
            success);

        Assert.True(
            usedPooledCopy);

        Assert.NotNull(
            entry);

        Assert.Equal(
            1001,
            entry.EventId);

        // Invalid multi-segment line: takes pooled-copy path but
        // fails parsing.
        ReadOnlySequence<byte> invalidLine =
            SegmentedSequence.Create(
                "invalid"u8
                    .ToArray(),

                "|2|"u8
                    .ToArray(),

                "1001|msg"u8
                    .ToArray());

        success =
            decoder.TryDecode(
                invalidLine,
                out entry,
                out usedPooledCopy);

        Assert.False(
            success);

        Assert.True(
            usedPooledCopy);

        Assert.Null(
            entry);
    }

    [Fact]
    public void
        Decoder_trims_cr_and_rejects_oversized_lines()
    {
        var decoder =
            CreateDecoder();

        ReadOnlySequence<byte> crlfLine =
            new(
                "1785924000|2|1001|Cache warmed\r"u8
                    .ToArray());

        Assert.True(
            decoder.TryDecode(
                crlfLine,
                out LogEntry? entry,
                out bool usedPooledCopy));

        Assert.NotNull(
            entry);

        Assert.False(
            usedPooledCopy);

        byte[] oversized =
            Enumerable.Repeat(
                    (byte)'a',
                    LogLineDecoder
                        .MaximumLineLength
                    + 1)
                .ToArray();

        Assert.False(
            decoder.TryDecode(
                new ReadOnlySequence<byte>(
                    oversized),
                out _,
                out bool oversizedPooledCopy));

        Assert.False(
            oversizedPooledCopy);
    }

    [Fact]
    public async Task
        Ingestor_handles_chunked_crlf_and_final_line()
    {
        const string input =
            "1785924000|2|1001|First\r\n"
            + "1785924001|4|1002|Second\n"
            + "1785924002|1|1003|Final";

        byte[] data =
            Encoding.UTF8.GetBytes(
                input);

        using var stream =
            new ChunkedReadStream(
                data,
                maximumChunkSize:
                    5);

        var accepted =
            new List<LogEntry>();

        LogIngestionResult result =
            await CreateIngestor()
                .IngestAsync(
                    stream,
                    accepted.Add,
                    TestContext.Current
                        .CancellationToken);

        Assert.Equal(
            3,
            result.TotalLines);

        Assert.Equal(
            3,
            result.ValidLines);

        Assert.Equal(
            0,
            result.InvalidLines);

        Assert.Collection(
            accepted,
            first =>
                Assert.Equal(
                    "First",
                    first.Message),
            second =>
                Assert.Equal(
                    "Second",
                    second.Message),
            final =>
                Assert.Equal(
                    "Final",
                    final.Message));
    }

    [Fact]
    public async Task
        Ingestor_handles_oversized_and_invalid_and_empty_lines()
    {
        // --- oversized record across repeated small stream reads ---
        // 6000 'A's produce > 4098 bytes without LF, triggering the
        // ingestor's oversized-discard path across PipeReader reads.
        byte[] oversizedRecord =
            Enumerable.Repeat(
                    (byte)'A',
                    6_000)
                .ToArray();

        byte[] validRecord =
            "1|1|1|valid\n"u8
                .ToArray();

        byte[] allData =
            new byte[
                oversizedRecord.Length
                + 1
                + validRecord.Length];

        oversizedRecord.CopyTo(
            allData,
            0);

        allData[
            oversizedRecord.Length] =
            (byte)'\n';

        validRecord.CopyTo(
            allData,
            oversizedRecord.Length
            + 1);

        using var stream =
            new ChunkedReadStream(
                allData,
                maximumChunkSize:
                    2048);

        var accepted =
            new List<LogEntry>();

        LogIngestionResult result =
            await CreateIngestor()
                .IngestAsync(
                    stream,
                    accepted.Add,
                    TestContext.Current
                        .CancellationToken);

        // --- invalid-and-empty-lines data ---
        const string secondInput =
            "1785924000|2|1001|Valid\n"
            + "\n"
            + "invalid\n"
            + "1785924001|1|1002|Also valid\n";

        using var stream2 =
            new MemoryStream(
                Encoding.UTF8
                    .GetBytes(
                        secondInput),
                writable:
                    false);

        var accepted2 =
            new List<LogEntry>();

        LogIngestionResult result2 =
            await CreateIngestor()
                .IngestAsync(
                    stream2,
                    accepted2.Add,
                    TestContext.Current
                        .CancellationToken);

        // --- Oversized-record assertions ---
        // total=2 (1 discarded + 1 valid), valid=1, invalid=1
        Assert.Equal(
            2,
            result.TotalLines);

        Assert.Equal(
            1,
            result.ValidLines);

        Assert.Equal(
            1,
            result.InvalidLines);

        Assert.Single(
            accepted);

        Assert.Equal(
            "valid",
            accepted[0].Message);

        // --- Second-stream assertions ---
        Assert.Equal(
            4,
            result2.TotalLines);

        Assert.Equal(
            2,
            result2.ValidLines);

        Assert.Equal(
            2,
            result2.InvalidLines);

        Assert.Equal(
            2,
            accepted2.Count);

        Assert.Equal(
            "Valid",
            accepted2[0].Message);

        Assert.Equal(
            "Also valid",
            accepted2[1].Message);
    }

    [Fact]
    public async Task
        Ingestor_leaves_caller_stream_open()
    {
        byte[] data =
            "1785924000|2|1001|Valid\n"u8
                .ToArray();

        using var stream =
            new MemoryStream(
                data,
                writable:
                    false);

        await CreateIngestor()
            .IngestAsync(
                stream,
                _ =>
                {
                },
                TestContext.Current
                    .CancellationToken);

        Assert.True(
            stream.CanRead);

        stream.Position =
            0;

        Assert.Equal(
            (int)'1',
            stream.ReadByte());
    }

    [Fact]
    public async Task
        Ingestor_propagates_caller_cancellation()
    {
        using var stream =
            new BlockingReadStream();

        using var cancellation =
            new CancellationTokenSource();

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
                () =>
                    CreateIngestor()
                        .IngestAsync(
                            stream,
                            _ =>
                            {
                            },
                            cancellation.Token));
    }

    private static LogLineDecoder
        CreateDecoder() =>
            new(
                new Utf8LogLineParser());

    private static LogIngestor
        CreateIngestor() =>
            new(
                CreateDecoder());
}