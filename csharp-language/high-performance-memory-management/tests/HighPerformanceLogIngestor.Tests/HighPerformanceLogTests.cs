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
    }

    [Fact]
    public void
        Decoder_uses_pooled_copy_for_multisegment_line()
    {
        var decoder =
            CreateDecoder();

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
        Ingestor_counts_invalid_and_empty_lines()
    {
        const string input =
            "1785924000|2|1001|Valid\n"
            + "\n"
            + "invalid\n"
            + "1785924001|1|1002|Also valid\n";

        using var stream =
            new MemoryStream(
                Encoding.UTF8
                    .GetBytes(
                        input),
                writable:
                    false);

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
            4,
            result.TotalLines);

        Assert.Equal(
            2,
            result.ValidLines);

        Assert.Equal(
            2,
            result.InvalidLines);

        Assert.Equal(
            2,
            accepted.Count);
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