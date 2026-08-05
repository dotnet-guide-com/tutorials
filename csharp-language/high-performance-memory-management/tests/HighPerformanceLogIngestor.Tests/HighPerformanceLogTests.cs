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
        // --- oversized record: 7000 'A's + LF + valid follower ---
        // 7000 bytes ensures the retained buffer exceeds
        // MaxRetainedBeforeNewline (4097) while no LF is present,
        // exercising the ingestor-level discard path.
        byte[] oversizedRecord =
            Enumerable.Repeat(
                    (byte)'A',
                    7_000)
                .ToArray();

        byte[] oversizedValid =
            "1|1|1|validA\n"u8
                .ToArray();

        byte[] oversizedData =
            new byte[
                oversizedRecord.Length
                + 1
                + oversizedValid.Length];

        oversizedRecord.CopyTo(
            oversizedData,
            0);

        oversizedData[
            oversizedRecord.Length] =
            (byte)'\n';

        oversizedValid.CopyTo(
            oversizedData,
            oversizedRecord.Length
            + 1);

        using var oversizedStream =
            new ChunkedReadStream(
                oversizedData,
                maximumChunkSize:
                    2048);

        var oversizedAccepted =
            new List<LogEntry>();

        LogIngestionResult oversizedResult =
            await CreateIngestor()
                .IngestAsync(
                    oversizedStream,
                    oversizedAccepted.Add,
                    TestContext.Current
                        .CancellationToken);

        // --- exact 4096-byte LF-terminated record + valid follower ---
        // "1785924000|2|1001|" = 22 bytes; pad message to 4096 total.
        byte[] prefix =
            "1785924000|2|1001|"u8
                .ToArray();

        const int boundaryLength =
            LogLineDecoder.MaximumLineLength;

        int messagePad =
            boundaryLength
            - prefix.Length;

        byte[] boundaryLF =
            new byte[
                prefix.Length
                + messagePad
                + 1
                + oversizedValid.Length];

        prefix.CopyTo(
            boundaryLF,
            0);

        Array.Fill<byte>(
            boundaryLF,
            (byte)'x',
            prefix.Length,
            messagePad);

        boundaryLF[
            boundaryLength] =
            (byte)'\n';

        oversizedValid.CopyTo(
            boundaryLF,
            boundaryLength
            + 1);

        using var boundaryLFStream =
            new ChunkedReadStream(
                boundaryLF,
                maximumChunkSize:
                    2_048);

        var boundaryLFAccepted =
            new List<LogEntry>();

        LogIngestionResult boundaryLFResult =
            await CreateIngestor()
                .IngestAsync(
                    boundaryLFStream,
                    boundaryLFAccepted.Add,
                    TestContext.Current
                        .CancellationToken);

        // --- exact 4096-byte CRLF-terminated record + valid follower ---
        byte[] boundaryCRLF =
            new byte[
                prefix.Length
                + messagePad
                + 2
                + oversizedValid.Length];

        prefix.CopyTo(
            boundaryCRLF,
            0);

        Array.Fill<byte>(
            boundaryCRLF,
            (byte)'x',
            prefix.Length,
            messagePad);

        boundaryCRLF[
            boundaryLength] =
            (byte)'\r';

        boundaryCRLF[
            boundaryLength
            + 1] =
            (byte)'\n';

        oversizedValid.CopyTo(
            boundaryCRLF,
            boundaryLength
            + 2);

        using var boundaryCRLFStream =
            new ChunkedReadStream(
                boundaryCRLF,
                maximumChunkSize:
                    2_048);

        var boundaryCRLFAccepted =
            new List<LogEntry>();

        LogIngestionResult boundaryCRLFResult =
            await CreateIngestor()
                .IngestAsync(
                    boundaryCRLFStream,
                    boundaryCRLFAccepted.Add,
                    TestContext.Current
                        .CancellationToken);

        // --- invalid-and-empty-lines data ---
        const string invalidInput =
            "1785924000|2|1001|Valid\n"
            + "\n"
            + "invalid\n"
            + "1785924001|1|1002|Also valid\n";

        using var invalidStream =
            new MemoryStream(
                Encoding.UTF8
                    .GetBytes(
                        invalidInput),
                writable:
                    false);

        var invalidAccepted =
            new List<LogEntry>();

        LogIngestionResult invalidResult =
            await CreateIngestor()
                .IngestAsync(
                    invalidStream,
                    invalidAccepted.Add,
                    TestContext.Current
                        .CancellationToken);

        // --- Oversized-record assertions ---
        // total=2 (1 discarded + 1 valid), valid=1, invalid=1
        Assert.Equal(
            2,
            oversizedResult.TotalLines);

        Assert.Equal(
            1,
            oversizedResult.ValidLines);

        Assert.Equal(
            1,
            oversizedResult.InvalidLines);

        Assert.Single(
            oversizedAccepted);

        Assert.Equal(
            "validA",
            oversizedAccepted[0].Message);

        // --- 4096-byte LF boundary assertions ---
        Assert.Equal(
            2,
            boundaryLFResult.TotalLines);

        Assert.Equal(
            2,
            boundaryLFResult.ValidLines);

        Assert.Equal(
            0,
            boundaryLFResult.InvalidLines);

        Assert.Equal(
            2,
            boundaryLFAccepted.Count);

        Assert.Equal(
            "validA",
            boundaryLFAccepted[1].Message);

        // --- 4096-byte CRLF boundary assertions ---
        Assert.Equal(
            2,
            boundaryCRLFResult.TotalLines);

        Assert.Equal(
            2,
            boundaryCRLFResult.ValidLines);

        Assert.Equal(
            0,
            boundaryCRLFResult.InvalidLines);

        Assert.Equal(
            2,
            boundaryCRLFAccepted.Count);

        Assert.Equal(
            "validA",
            boundaryCRLFAccepted[1].Message);

        // --- Invalid-and-empty-lines assertions ---
        Assert.Equal(
            4,
            invalidResult.TotalLines);

        Assert.Equal(
            2,
            invalidResult.ValidLines);

        Assert.Equal(
            2,
            invalidResult.InvalidLines);

        Assert.Equal(
            2,
            invalidAccepted.Count);

        Assert.Equal(
            "Valid",
            invalidAccepted[0].Message);

        Assert.Equal(
            "Also valid",
            invalidAccepted[1].Message);
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